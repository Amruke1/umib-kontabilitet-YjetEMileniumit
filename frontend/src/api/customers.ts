import type { Customer } from '../types/Customer'

const apiUrl = (import.meta.env.VITE_API_URL ?? 'http://localhost:5080').replace(/\/$/, '')
export type NewCustomer = Omit<Customer, 'id'>

async function request<T>(init?: RequestInit): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${apiUrl}/api/customers`, init)
  } catch {
    throw new Error('API-ja nuk është e qasshme. Kontrolloni nëse backend-i është nisur dhe provoni përsëri.')
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => null)
    const errors = problem?.errors as Record<string, string[]> | undefined
    throw new Error(errors ? Object.values(errors).flat().join(' ') : 'Kërkesa dështoi. Provoni përsëri.')
  }
  return response.json() as Promise<T>
}

export const getCustomers = () => request<Customer[]>()
export const addCustomer = (customer: NewCustomer) => request<Customer>({
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(customer),
})
