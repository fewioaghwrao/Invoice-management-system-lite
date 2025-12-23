import { NextResponse } from "next/server";
import { cookies } from "next/headers";

const baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5093";

export async function GET(
  _req: Request,
  { params }: { params: Promise<{ id: string }> }
) {
  const { id } = await params;

  const c = await cookies();
  const token = c.get("token")?.value ?? null;

  // 未ログインなら 401（proxy.ts が守っているなら実際には起きにくい）
  if (!token) {
    return NextResponse.json({ message: "Unauthorized" }, { status: 401 });
  }

  // ✅ 会員スコープのPDF APIへ転送
  const url = `${baseUrl}/api/members/me/invoices/${id}/pdf`;

  const res = await fetch(url, {
    method: "GET",
    headers: {
      Authorization: `Bearer ${token}`,
    },
    cache: "no-store",
  });

  if (!res.ok) {
    const text = await res.text().catch(() => "");
    console.error("PDF proxy error:", res.status, text);
    return NextResponse.json(
      { message: `PDF fetch failed: ${res.status}` },
      { status: res.status }
    );
  }

  const arrayBuffer = await res.arrayBuffer();

  // 受け取ったヘッダを極力引き継ぐ
  const contentType = res.headers.get("content-type") ?? "application/pdf";
  const contentDisposition =
    res.headers.get("content-disposition") ??
    `inline; filename="invoice-${id}.pdf"`;

  return new NextResponse(arrayBuffer, {
    status: 200,
    headers: {
      "Content-Type": contentType,
      "Content-Disposition": contentDisposition,
      // PDFはキャッシュしない方が無難
      "Cache-Control": "no-store",
    },
  });
}
