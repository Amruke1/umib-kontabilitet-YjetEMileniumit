import type { Customer } from '../types/Customer'
import { apiRequest } from './http'

export type NewCustomer = Omit<Customer, 'id'>

const request = <T,>(init?: RequestInit, path = '') => apiRequest<T>(`/api/customers${path}`, init)

export const getCustomers = () => request<Customer[]>()
export const addCustomer = (customer: NewCustomer) => request<Customer>({
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(customer),
})

export const updateCustomer = (id: number, customer: NewCustomer) => request<Customer>({
  method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(customer),
}, `/${id}`)
export const deleteCustomer = (id: number) => request<void>({ method: 'DELETE' }, `/${id}`)
