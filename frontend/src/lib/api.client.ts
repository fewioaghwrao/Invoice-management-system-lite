// src/lib/api.client.ts
const baseUrl =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5093";

function getTokenFromLocalStorage(): string | null {
  const raw = localStorage.getItem("currentUser");
  if (!raw) return null;
  try {
    return JSON.parse(raw)?.token ?? null;
  } catch {
    return null;
  }
}

function buildUrl(path: string, query?: Record<string, string | number | undefined>) {
  const url = new URL(`${baseUrl}${path}`);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== "") {
        url.searchParams.append(key, String(value));
      }
    }
  }
  return url;
}

async function handle(res: Response) {
  if (!res.ok) {
    const text = await res.text().catch(() => "");
    console.error("API error response:", res.status, text);
    throw new Error(`API error: ${res.status} ${text}`);
  }
}

export async function apiGetClient<T>(
  path: string,
  query?: Record<string, string | number | undefined>
): Promise<T> {
  const url = buildUrl(path, query);
  const token = getTokenFromLocalStorage();

  const res = await fetch(url.toString(), {
    cache: "no-store",
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });

  await handle(res);
  return res.json() as Promise<T>;
}

export async function apiDeleteClient(path: string): Promise<void> {
  const token = getTokenFromLocalStorage();
  const res = await fetch(`${baseUrl}${path}`, {
    method: "DELETE",
    cache: "no-store",
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });
  await handle(res);
}

export async function apiPutClient<TBody>(path: string, body: TBody): Promise<void> {
  const token = getTokenFromLocalStorage();
  const res = await fetch(`${baseUrl}${path}`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify(body),
    cache: "no-store",
  });
  await handle(res);
}

export async function apiPostClient<TResponse = any, TBody = any>(
  path: string,
  body?: TBody
): Promise<TResponse> {
  const token = getTokenFromLocalStorage();
  const res = await fetch(`${baseUrl}${path}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
    cache: "no-store",
  });

  await handle(res);

  if (res.status === 204) return undefined as TResponse;

  const ct = res.headers.get("content-type") ?? "";
  if (ct.includes("application/json")) return (await res.json()) as TResponse;
  return (await res.text()) as any;
}

export async function apiGetBlobClient(
  path: string,
  query?: Record<string, string | number | undefined>
): Promise<{ blob: Blob; filename?: string }> {
  const url = buildUrl(path, query);
  const token = getTokenFromLocalStorage();

  const res = await fetch(url.toString(), {
    cache: "no-store",
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });

  await handle(res);

  // Content-Disposition から filename を拾う（あれば）
  const cd = res.headers.get("content-disposition") ?? "";
  const m = cd.match(/filename\*=UTF-8''([^;]+)|filename="([^"]+)"/);
  const filename = m ? decodeURIComponent(m[1] ?? m[2]) : undefined;

  const blob = await res.blob();
  return { blob, filename };
}