import { NextRequest, NextResponse } from "next/server";

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5093";

export async function PUT(req: NextRequest, ctx: any) {
  // Next16: params が Promise の場合がある
  const params = await ctx.params;
  const id = String(params?.id);

  if (!id) {
    return NextResponse.json({ message: "Missing id" }, { status: 400 });
  }

  const token = req.cookies.get("token")?.value;
  if (!token) {
    return NextResponse.json({ message: "Missing token cookie" }, { status: 401 });
  }

  const body = await req.text();

  const res = await fetch(`${API_BASE}/api/members/${id}`, {
    method: "PUT",
    headers: {
      "Content-Type": req.headers.get("content-type") ?? "application/json",
      Authorization: `Bearer ${token}`,
    },
    body,
    cache: "no-store",
  });

  const text = await res.text().catch(() => "");
  return new NextResponse(text, {
    status: res.status,
    headers: { "Content-Type": res.headers.get("content-type") ?? "application/json" },
  });
}
