import { apiRequest } from './http'
import type { Role, UserAccount } from '../types/User'

const json = (body: unknown) => ({ headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })

export const getSession = () => apiRequest<UserAccount>('/api/auth/me')
export const getSetupStatus = () => apiRequest<{ required: boolean; available: boolean }>('/api/auth/setup')
export const login = (userName: string, password: string) =>
  apiRequest<UserAccount>('/api/auth/login', { method: 'POST', ...json({ userName, password }) })
export const logout = () => apiRequest<void>('/api/auth/logout', { method: 'POST' })
export const setupAdministrator = (input: { userName: string; displayName: string; password: string; setupToken: string }) =>
  apiRequest<UserAccount>('/api/auth/setup', { method: 'POST', ...json(input) })
export const changePassword = (currentPassword: string, newPassword: string) =>
  apiRequest<void>('/api/auth/password', { method: 'POST', ...json({ currentPassword, newPassword }) })
export const getUsers = () => apiRequest<UserAccount[]>('/api/users')
export const createUser = (input: { userName: string; displayName: string; password: string; role: Role }) =>
  apiRequest<UserAccount>('/api/users', { method: 'POST', ...json(input) })
export const updateUser = (id: number, input: { displayName: string; role: Role; isActive: boolean }) =>
  apiRequest<UserAccount>(`/api/users/${id}`, { method: 'PUT', ...json(input) })
