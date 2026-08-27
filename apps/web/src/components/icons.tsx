import type { ReactNode, SVGProps } from 'react'

type IconProps = SVGProps<SVGSVGElement> & { size?: number }

function Icon({ size = 18, children, ...props }: IconProps & { children: ReactNode }) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
      {...props}
    >
      {children}
    </svg>
  )
}

export const EyeIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M1 12s4-7 11-7 11 7 11 7-4 7-11 7-11-7-11-7Z" />
    <circle cx={12} cy={12} r={3} />
  </Icon>
)

export const EyeOffIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M3 3l18 18" />
    <path d="M10.6 10.6a3 3 0 0 0 4.24 4.24" />
    <path d="M6.6 6.7C4 8.3 2 12 2 12s4 7 11 7c1.7 0 3.2-.4 4.5-1" />
    <path d="M17.4 17.3C20 15.7 22 12 22 12s-1-1.8-2.7-3.5" />
  </Icon>
)

export const ArrowLeftIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M19 12H5" />
    <path d="M11 18l-6-6 6-6" />
  </Icon>
)

export const BellIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M6 8a6 6 0 1 1 12 0c0 4 1.5 5.5 2 6.5H4c.5-1 2-2.5 2-6.5Z" />
    <path d="M10 19a2 2 0 0 0 4 0" />
  </Icon>
)

export const ChevronDownIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M6 9l6 6 6-6" />
  </Icon>
)

export const LogoutIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
    <path d="M16 17l5-5-5-5" />
    <path d="M21 12H9" />
  </Icon>
)

export const SearchIcon = (p: IconProps) => (
  <Icon {...p}>
    <circle cx={11} cy={11} r={7} />
    <path d="M21 21l-4.3-4.3" />
  </Icon>
)

export const TruckIcon = (p: IconProps) => (
  <Icon {...p}>
    <rect x={1} y={9} width={15} height={8} rx={0} />
    <path d="M16 13h3l3 3v2h-6" />
    <circle cx={6} cy={19} r={1.6} />
    <circle cx={17.5} cy={19} r={1.6} />
  </Icon>
)

export const WrenchIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M14.7 6.3a1 1 0 0 0 1.4 0l1.6-1.6a1 1 0 0 0 0-1.4L16.4 2a5 5 0 1 0-6.4 6.4l-8 8a2 2 0 1 0 2.8 2.8l8-8a5 5 0 0 0 1.9-4.9Z" />
  </Icon>
)

export const UsersIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
    <circle cx={9} cy={7} r={4} />
    <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
    <path d="M16 3.13a4 4 0 0 1 0 7.75" />
  </Icon>
)

export const RouteIcon = (p: IconProps) => (
  <Icon {...p}>
    <circle cx={6} cy={19} r={3} />
    <path d="M9 19h8.5a3.5 3.5 0 0 0 0-7h-11a3.5 3.5 0 0 1 0-7H15" />
    <circle cx={18} cy={5} r={3} />
  </Icon>
)

export const GridIcon = (p: IconProps) => (
  <Icon {...p}>
    <rect x={3} y={3} width={7} height={7} />
    <rect x={14} y={3} width={7} height={7} />
    <rect x={3} y={14} width={7} height={7} />
    <rect x={14} y={14} width={7} height={7} />
  </Icon>
)

export const MailIcon = (p: IconProps) => (
  <Icon {...p}>
    <rect x={2} y={4} width={20} height={16} rx={0} />
    <path d="m2 6 10 7 10-7" />
  </Icon>
)

export const ChevronLeftIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M15 18l-6-6 6-6" />
  </Icon>
)

export const ChevronRightIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M9 18l6-6-6-6" />
  </Icon>
)

export const WhatsappIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M12 4a8 8 0 0 1 8 8c0 4.4-3.6 8-8 8a7.9 7.9 0 0 1-4-1.1L4 20l1.2-4A7.9 7.9 0 0 1 4 12a8 8 0 0 1 8-8Z" />
    <path d="M9 9.5c0 3 2.5 5.5 5.5 5.5" />
  </Icon>
)

export const InstagramIcon = (p: IconProps) => (
  <Icon {...p}>
    <rect x={3} y={3} width={18} height={18} rx={0} />
    <circle cx={12} cy={12} r={4.5} />
    <circle cx={17} cy={7} r={0.7} fill="currentColor" />
  </Icon>
)

export const PencilIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7.5 18.5 3 20l1.5-4.5 12-12Z" />
    <path d="M14.5 5.5l4 4" />
  </Icon>
)

export const TrashIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M3 6h18" />
    <path d="M8 6V4h8v2" />
    <path d="M5 6v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V6" />
    <path d="M10 11v6M14 11v6" />
  </Icon>
)

export const CheckIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M20 6L9 17l-5-5" />
  </Icon>
)

export const XIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M18 6L6 18M6 6l12 12" />
  </Icon>
)

export const ClipboardIcon = (p: IconProps) => (
  <Icon {...p}>
    <rect x={4} y={4} width={16} height={17} />
    <path d="M8 4V2.5h8V4" />
    <path d="M8 10h8M8 14h8M8 18h5" />
  </Icon>
)

export const AlertIcon = (p: IconProps) => (
  <Icon {...p}>
    <path d="M12 3 1.8 20.5h20.4L12 3Z" />
    <path d="M12 10v4" />
    <path d="M12 17.2v.1" />
  </Icon>
)
