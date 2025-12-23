// src/app/collections/[invoiceId]/page.tsx
import CollectionsClient from "./CollectionsClient";

export default async function CollectionsPage({
  params,
}: {
  params: Promise<{ invoiceId: string }>;
}) {
  const { invoiceId } = await params;
  return <CollectionsClient invoiceId={invoiceId} />;
}
