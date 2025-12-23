// src/app/api/account/me/route.ts
import { NextResponse } from "next/server";
import { cookies } from "next/headers";

function getApiBase() {
  const apiBase = process.env.NEXT_PUBLIC_API_BASE_URL;
  if (!apiBase) throw new Error("NEXT_PUBLIC_API_BASE_URL is not set");
  return apiBase;
}

async function getTokenOrThrow() {
  const c = await cookies();
  const token = c.get("token")?.value;
  if (!token) throw new Error("token cookie not found");
  return token;
}

export async function GET() {
  const apiBase = getApiBase();
  const token = await getTokenOrThrow();

  const res = await fetch(`${apiBase}/api/members/me`, {
    method: "GET",
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });

  const text = await res.text().catch(() => "");
  if (!res.ok) {
    return NextResponse.json(
      { message: "backend error", status: res.status, body: text },
      { status: res.status }
    );
  }
  return NextResponse.json(JSON.parse(text));
}

export async function PUT(req: Request) {
  const apiBase = getApiBase();
  const token = await getTokenOrThrow();
  const body = await req.text().catch(() => "");

  const res = await fetch(`${apiBase}/api/members/me`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body,
    cache: "no-store",
  });

  const text = await res.text().catch(() => "");
  if (!res.ok) {
    return NextResponse.json(
      { message: "backend error", status: res.status, body: text },
      { status: res.status }
    );
  }
  return NextResponse.json(JSON.parse(text));
}

export async function DELETE() {
  const apiBase = getApiBase();
  const token = await getTokenOrThrow();

  const res = await fetch(`${apiBase}/api/members/me`, {
    method: "DELETE",
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });

  const text = await res.text().catch(() => "");
  if (!res.ok) {
    return NextResponse.json(
      { message: "backend error", status: res.status, body: text },
      { status: res.status }
    );
  }

  return new NextResponse(null, { status: 204 });
}
