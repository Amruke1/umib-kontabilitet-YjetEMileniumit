export type Role = 'Administrator' | 'Salesperson' | 'Warehouse' | 'Accountant'

export type UserAccount = {
  id: number
  userName: string
  displayName: string
  role: Role
  isActive: boolean
}

export const roleLabels: Record<Role, string> = {
  Administrator: 'Administratori',
  Salesperson: 'Shitësi',
  Warehouse: 'Magazinieri',
  Accountant: 'Kontabilisti',
}

export const canReadCustomers = (user: UserAccount) =>
  ['Administrator', 'Salesperson', 'Accountant'].includes(user.role)

export const canWriteCustomers = (user: UserAccount) =>
  ['Administrator', 'Salesperson'].includes(user.role)
