// src/app/invoices/new/page.tsx
import InvoiceFormClient from "../shared/InvoiceFormClient";

export default async function InvoiceNewPage({
  searchParams,
}: {
  searchParams: Promise<{ from?: string }>;
}) {
  const sp = await searchParams;
  const from = sp.from ?? "";
  return <InvoiceFormClient mode="new" from={from} />;
}
