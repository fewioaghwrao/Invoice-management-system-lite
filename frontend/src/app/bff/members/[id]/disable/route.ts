import { NextRequest, NextResponse } from "next/server";

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5093";

export async function PUT(req: NextRequest, ctx: any) {
  const params = await ctx.params;
  const id = String(params?.id);

  if (!id) {
    return NextResponse.json({ message: "Missing id" }, { status: 400 });
  }

  const token = req.cookies.get("token")?.value;
  if (!token) {
    return NextResponse.json({ message: "Missing token cookie" }, { status: 401 });
  }

  const res = await fetch(`${API_BASE}/api/members/${id}/disable`, {
    method: "PUT",
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });

  const text = await res.text().catch(() => "");
  return new NextResponse(text, {
    status: res.status,
    headers: { "Content-Type": res.headers.get("content-type") ?? "application/json" },
  });
}

