# -*- coding: utf-8 -*-
"""Gera docs/arquitetura.png — o diagrama de arquitetura da API Frota360.

As quatro camadas, o fluxo CQRS de um request e os cinco pontos de
isolamento por EmpresaId, num desenho só. Rótulos em inglês, paleta
monocromática.

Uso:
    python -m pip install pillow
    python docs/arquitetura.py

Depende das fontes Segoe UI e Consolas (padrão do Windows).
"""
from PIL import Image, ImageDraw, ImageFont
import os

F = "C:/Windows/Fonts/"
def f(name, size): return ImageFont.truetype(F + name, size)

UI   = lambda s: f("segoeui.ttf", s)
UIB  = lambda s: f("segoeuib.ttf", s)
MON  = lambda s: f("consola.ttf", s)
MONB = lambda s: f("consolab.ttf", s)

# ---- paleta monocromática -------------------------------------------------
PAPER  = "#FCFCFB"
INK    = "#161615"
INK2   = "#3B3B39"
GRAY   = "#6E6E6B"
GRAY_L = "#9B9B97"
RULE   = "#CFCFCA"
WHITE  = "#FFFFFF"
BANDS  = ["#F7F7F5", "#F1F1EE", "#EBEBE7", "#E4E4DF"]

W, H = 2400, 2000
img = Image.new("RGB", (W, H), PAPER)
d = ImageDraw.Draw(img)

def tw(t, fnt): return d.textlength(t, font=fnt)

def arrow_down(x, y1, y2, color="#6E6E6B", width=3):
    d.line([x, y1, x, y2 - 11], fill=color, width=width)
    d.polygon([(x - 8, y2 - 12), (x + 8, y2 - 12), (x, y2)], fill=color)

def num_box(x, y, n, size=34):
    d.rectangle([x, y, x + size, y + size], fill=INK)
    d.text((x + size / 2, y + size / 2 + 1), str(n), font=UIB(size - 12),
           fill=WHITE, anchor="mm")

# ---- cabeçalho ------------------------------------------------------------
X0, X1 = 60, 2340
d.text((62, 44), "Frota360 \u2014 API Architecture", font=UIB(56), fill=INK)
d.text((66, 116),
       "Clean Architecture across four layers   \u00b7   hand-rolled CQRS via Dispatcher   \u00b7   per-tenant isolation on EmpresaId",
       font=UI(26), fill=GRAY)
d.line([X0, 172, X1, 172], fill=RULE, width=2)

SPLIT = 960
LX, LW = 92, 830
NX0, NX1 = 1000, 1620
BADGE_X = 1660
AX = 1712

d.text((62, 192), "T H E   F O U R   L A Y E R S", font=UIB(20), fill=GRAY_L)
d.text((NX0, 192), "R E Q U E S T   F L O W   ( C Q R S )", font=UIB(20), fill=GRAY_L)

LAYERS = [
    dict(y0=232, y1=532, no="01", name="Frota360", tag="(Api)",
         items=["Controllers",
                "ExceptionMiddleware",
                "CurrentUserService",
                "Program / dependency injection"],
         note="references \u2192  Application + Infrastructure"),
    dict(y0=568, y1=960, no="02", name="Frota360.Application", tag="",
         items=["UseCases/  \u2014  Commands \u00b7 Queries \u00b7 Handlers",
                "Services/  \u2014  auth \u00b7 invite \u00b7 user \u00b7 backoffice",
                "DTOs/  Request + Response",
                "Validators (FluentValidation)",
                "Mappings  \u2014  ToResponse()"],
         note="references \u2192  Domain          never \u2192  EF Core \u00b7 ASP.NET \u00b7 DbContext"),
    dict(y0=996, y1=1296, no="03", name="Frota360.Infrastructure", tag="",
         items=["Frota360DbContext  (EF Core + SQL Server)",
                "Repositories",
                "TokenService (JWT)",
                "E-mail delivery (Resend)",
                "Migrations"],
         note="references \u2192  Domain          never \u2192  Application"),
    dict(y0=1332, y1=1568, no="04", name="Frota360.Domain", tag="",
         items=["Entities  (Veiculo \u00b7 Motorista \u00b7 Rota \u00b7 Manutencao \u2026)",
                "Enums \u00b7 Roles \u00b7 ApiResponse<T>",
                "Repository and service interfaces"],
         note="no dependencies \u2014 zero packages"),
]

for i, L in enumerate(LAYERS):
    d.rounded_rectangle([X0, L["y0"], X1, L["y1"]], radius=3,
                        fill=BANDS[i], outline=RULE, width=2)
    d.rectangle([X0, L["y0"], X0 + 7, L["y1"]], fill=INK)
    d.line([SPLIT, L["y0"] + 18, SPLIT, L["y1"] - 18], fill=RULE, width=2)

    y = L["y0"] + 26
    d.text((LX, y + 8), L["no"], font=UIB(20), fill=GRAY_L)
    d.text((LX + 52, y), L["name"], font=UIB(34), fill=INK)
    if L["tag"]:
        d.text((LX + 52 + tw(L["name"], UIB(34)) + 14, y + 9), L["tag"],
               font=UI(25), fill=GRAY)
    y += 62
    for it in L["items"]:
        d.rectangle([LX + 4, y + 11, LX + 11, y + 18], fill=GRAY_L)
        d.text((LX + 28, y + 15), it, font=UI(23), fill=INK2, anchor="lm")
        y += 32
    d.text((LX + 4, y + 16), L["note"], font=UI(21), fill=GRAY)

for i in range(3):
    arrow_down(LX + 3, LAYERS[i]["y1"] + 4, LAYERS[i + 1]["y0"] - 2, GRAY_L, 3)

# ---- fluxo CQRS -----------------------------------------------------------
# (camada, título, subtítulo, monoespaçado?, anotação, nº do ponto de tenant)
NODES = [
    (0, "HTTP request  +  Bearer JWT", None, False,
        "The token carries the empresaId claim", 1),
    (0, "Middleware pipeline",
        "ExceptionMiddleware \u00b7 Serilog \u00b7 CORS \u00b7 RateLimiter \u00b7 AuthN/Z", False,
        "InvalidOperationException \u2192 422", None),
    (0, "Controller",
        "validates IValidator<T> \u2192 400   \u00b7   wraps in ApiResponse<T>", False,
        "Handlers never build ApiResponse", None),
    (1, "dispatcher.SendAsync(new XCommand(request))", None, True,
        "The command carries the whole DTO in Data", None),
    (1, "Dispatcher",
        "resolves IRequestHandler<TReq,TResp> from DI (reflection)", False,
        "AddCqrsHandlers scans the assembly:\na new handler needs no registration", None),
    (1, "Handler (sealed)", "var empresaId = currentUser.EmpresaId", False,
        "Never from the body, route or query string", 2),
    (1, "entidade.ToResponse()", "manual mapping \u2014 no AutoMapper", False,
        "XResponse never exposes EmpresaId", 3),
    (2, "IXRepository.GetByIdAsync(id, empresaId)", None, True,
        "Interface in Domain,\nimplementation in Infrastructure", 4),
    (2, "EF Core  \u2192  SQL Server",
        "no global query filter \u2014 filtering is each repository's duty", False,
        None, None),
    (3, "Business entity",
        "EmpresaId  +  composite unique index (EmpresaId, CPF)", False,
        "Exception: Usuario.Email is globally unique", 5),
]

NH, GAP = 72, 20
pos, by_layer = [], {}
for n in NODES:
    by_layer.setdefault(n[0], []).append(n)
for li in sorted(by_layer):
    group, L = by_layer[li], LAYERS[li]
    total = len(group) * NH + (len(group) - 1) * GAP
    y = L["y0"] + (L["y1"] - L["y0"] - total) / 2
    for n in group:
        pos.append((n, y))
        y += NH + GAP

for (n, y) in pos:
    li, txt, sub, mono, ann, tenant = n
    key = tenant is not None
    d.rounded_rectangle([NX0, y, NX1, y + NH], radius=3, fill=WHITE,
                        outline=INK if key else RULE, width=3 if key else 2)
    d.rectangle([NX0, y, NX0 + (9 if key else 5), y + NH], fill=INK if key else GRAY_L)
    if sub:
        d.text((NX0 + 30, y + 23), txt, font=(MONB(23) if mono else UIB(24)),
               fill=INK, anchor="lm")
        d.text((NX0 + 30, y + 51), sub, font=(MON(19) if mono else UI(19)),
               fill=GRAY, anchor="lm")
    else:
        d.text((NX0 + 30, y + NH / 2 + 1), txt,
               font=(MONB(23) if mono else UIB(24)), fill=INK, anchor="lm")
    if key:
        num_box(BADGE_X, y + NH / 2 - 17, tenant)
    if ann:
        lines = ann.split("\n")
        y_a = y + NH / 2 - (len(lines) - 1) * 13
        for ln in lines:
            d.text((AX, y_a), ln, font=(UIB(20) if key else UI(20)),
                   fill=INK if key else GRAY, anchor="lm")
            y_a += 26

for i in range(len(pos) - 1):
    arrow_down((NX0 + NX1) / 2, pos[i][1] + NH + 3, pos[i + 1][1] - 2, GRAY, 3)

# ---- ponto de isolamento por tenant --------------------------------------
LY0 = 1604
LY1 = LY0 + 290
d.rounded_rectangle([X0, LY0, X1, LY1], radius=3, fill=WHITE, outline=INK, width=3)
d.rectangle([X0, LY0, X0 + 14, LY1], fill=INK)

d.text((X0 + 54, LY0 + 26), "Tenant isolation on EmpresaId \u2014 the critical point",
       font=UIB(31), fill=INK)
d.text((X0 + 54, LY0 + 72),
       "The value ALWAYS comes from the empresaId claim in the JWT, through ICurrentUserService \u2014 never from the body, the route or the query string.",
       font=UI(22), fill=GRAY)
d.line([X0 + 54, LY0 + 106, X1 - 40, LY0 + 106], fill=RULE, width=2)

ITEMS = [
    "The JWT carries the empresaId claim; CurrentUserService is the single source of the value.",
    "Every handler injects ICurrentUserService and reads currentUser.EmpresaId; on create, EmpresaId = currentUser.EmpresaId.",
    "The response never exposes EmpresaId \u2014 the tenant does not leak outside the API.",
    "Every repository signature takes empresaId and filters on it; a FK coming from the request is resolved through GetByIdAsync(id, empresaId), so an id from another company simply does not exist.",
    "Unique indexes in the DbContext are composite with EmpresaId \u2014 (EmpresaId, CPF), (EmpresaId, Nome). Exception: Usuario.Email is globally unique.",
]
yy = LY0 + 126
for i, t in enumerate(ITEMS, 1):
    num_box(X0 + 54, yy, i, size=24)
    d.text((X0 + 92, yy + 13), t, font=UI(22), fill=INK2, anchor="lm")
    yy += 32

# ---- rodapé ---------------------------------------------------------------
d.line([X0, 1938, X1, 1938], fill=RULE, width=2)
d.text((X0, 1954), "docs/arquitetura.png  \u2014  generated by docs/arquitetura.py",
       font=UI(19), fill=GRAY_L)
d.text((X1, 1954), "Frota360 \u00b7 .NET 10", font=UI(19), fill=GRAY_L, anchor="ra")

out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "arquitetura.png")
os.makedirs(os.path.dirname(out), exist_ok=True)
img.save(out)
print("ok", img.size, "->", out)
