// src/app/invoices/[id]/edit/page.tsx
import InvoiceFormClient from "../../shared/InvoiceFormClient";
import { apiGetServer } from "@/lib/api.server";
import type { InvoiceDetailDto } from "@/types/invoice";

function toInitial(inv: InvoiceDetailDto) {
  const lines = (inv as any).lines as any[] | undefined;

  const items =
    lines?.length
      ? lines.map((x) => ({
          id: Number(x.id),           // ★追加：これを保持する
          name: String(x.name ?? ""),
          qty: Number(x.qty),
          unitPrice: Number(x.unitPrice),
        }))
      : [
          {
            id: undefined,            // ★新規扱い
            name: "（明細なし）",
            qty: 1,
            unitPrice: Number((inv as any).totalAmount ?? 0),
          },
        ];

  return {
    id: String((inv as any).id),
    invoiceNumber: String((inv as any).invoiceNumber ?? ""),
    memberId: Number((inv as any).memberId),
    statusId: Number((inv as any).statusId),
    invoiceDate: String((inv as any).invoiceDate).slice(0, 10),
    dueDate: String((inv as any).dueDate).slice(0, 10),
    notes: String((inv as any).remarks ?? ""),
    items,
  };
}


async function getInvoiceDetail(id: string): Promise<InvoiceDetailDto> {
  return apiGetServer<InvoiceDetailDto>(`/api/invoices/${id}`);
}

export default async function InvoiceEditPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ from?: string }>;
}) {
  const { id } = await params;
  const sp = await searchParams;
  const from = sp.from ?? "";

  const inv = await getInvoiceDetail(id);
  const initial = toInitial(inv);

  return (
    <InvoiceFormClient mode="edit" invoiceId={id} from={from} initial={initial} />
  );
}


