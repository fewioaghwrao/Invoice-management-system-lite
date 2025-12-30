import { NextResponse } from "next/server";
import { cookies } from "next/headers";

export const dynamic = "force-dynamic";

export async function POST(req: Request) {
  const apiBase = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5093";

  const token = (await cookies()).get("token")?.value;
  if (!token) {
    return NextResponse.json({ message: "token cookie missing" }, { status: 401 });
  }

  const body = await req.text();

  const res = await fetch(`${apiBase}/api/payments`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
      Accept: "application/json",
    },
    body,
  });

  const out = await res.text().catch(() => "");
  return new NextResponse(out, {
    status: res.status,
    headers: { "content-type": res.headers.get("content-type") ?? "application/json" },
  });
}
