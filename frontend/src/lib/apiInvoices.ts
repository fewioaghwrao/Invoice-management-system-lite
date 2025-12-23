import { apiGetClient } from  "./api.client";
import { Invoice } from './types';

export function getLatestInvoices() {
  return apiGetClient<Invoice[]>('/api/invoices?take=5');
}

export function getInvoices() {
  return apiGetClient<Invoice[]>('/api/invoices');
}
