import { NextResponse } from "next/server";
import { cookies } from "next/headers";

export const dynamic = "force-dynamic";

const baseUrl = (process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5093").replace(/\/+$/, "");

// ★ params は Promise 扱いになるので Promise 型で受ける
type Ctx = { params: Promise<{ id: string }> };

export async function GET(_req: Request, { params }: Ctx) {
  // ★ここが必須
  const { id } = await params;

  const c = await cookies();
  const token = c.get("token")?.value ?? null;

  if (!token) {
    return NextResponse.json({ message: "Unauthorized (token missing)" }, { status: 401 });
  }

  const url = `${baseUrl}/api/invoices/${encodeURIComponent(id)}/pdf`;

  const res = await fetch(url, {
    method: "GET",
    headers: {
      Authorization: `Bearer ${token}`,
      Accept: "application/pdf",
    },
    cache: "no-store",
  });

  if (!res.ok) {
    const text = await res.text().catch(() => "");
    console.error("[admin pdf] backend error:", res.status, text);
    return NextResponse.json(
      { message: `PDF fetch failed: ${res.status}`, detail: text },
      { status: res.status }
    );
  }

  const arrayBuffer = await res.arrayBuffer();

  return new NextResponse(arrayBuffer, {
    status: 200,
    headers: {
      "Content-Type": res.headers.get("content-type") ?? "application/pdf",
      "Content-Disposition": res.headers.get("content-disposition") ?? `inline; filename="invoice-${id}.pdf"`,
      "Cache-Control": "no-store",
    },
  });
}

