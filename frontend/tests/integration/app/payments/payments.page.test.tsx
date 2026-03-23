import { render, screen } from "@testing-library/react";
import PaymentsPage from "@/app/payments/page";
import * as nextHeaders from "next/headers";

const fetchMock = jest.fn();

jest.mock("next/headers", () => ({
  cookies: jest.fn(),
}));

jest.mock("@/components/CurrentUserBadge", () => {
  return function MockCurrentUserBadge() {
    return <div>CurrentUserBadge</div>;
  };
});

jest.mock("@/components/LogoutButton", () => {
  return function MockLogoutButton() {
    return <button>LogoutButton</button>;
  };
});

describe("PaymentsPage", () => {
  const originalFetch = global.fetch;
  const mockCookies = nextHeaders.cookies as jest.MockedFunction<
    typeof nextHeaders.cookies
  >;

  beforeEach(() => {
    jest.clearAllMocks();

    Object.defineProperty(global, "fetch", {
      writable: true,
      value: fetchMock,
    });

    mockCookies.mockResolvedValue({
      get: (name: string) =>
        name === "token" ? { name: "token", value: "dummy-token" } : undefined,
      getAll: () => [{ name: "token", value: "dummy-token" }],
    } as any);
  });

  afterEach(() => {
    global.fetch = originalFetch;
  });

  it("入金一覧・サマリー・請求書リンクを表示する", async () => {
    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        year: 2026,
        month: "all",
        keyword: "株式会社",
        status: "all",
        page: 1,
        pageSize: 10,
        totalCount: 2,
        rows: [
          {
            id: 12,
            paymentDate: "2026-03-15T00:00:00",
            payerName: "株式会社A",
            amount: 300000,
            allocatedAmount: 120000,
            invoices: [
              { id: 10, invoiceNumber: "INV-0010" },
              { id: 11, invoiceNumber: "INV-0011" },
            ],
            status: "PARTIAL",
          },
          {
            id: 13,
            paymentDate: "2026-03-18T00:00:00",
            payerName: "株式会社B",
            amount: 500000,
            allocatedAmount: 500000,
            invoices: [{ id: 20, invoiceNumber: "INV-0020" }],
            status: "ALLOCATED",
          },
        ],
        summary: {
          totalAmount: 800000,
          allocatedTotal: 620000,
          unallocatedTotal: 180000,
        },
      }),
      status: 200,
      statusText: "OK",
    });

    const element = await PaymentsPage({
      searchParams: Promise.resolve({
        year: "2026",
        month: "all",
        q: "株式会社",
        status: "all",
        page: "1",
      }),
    });

    render(element);

    expect(screen.getByText("入金一覧")).toBeInTheDocument();
    expect(
      screen.getByText(
        "入金明細を一覧で確認し、請求書への割当状況（未割当/一部/完了）を把握できます。"
      )
    ).toBeInTheDocument();

    expect(screen.getByText("CurrentUserBadge")).toBeInTheDocument();
    expect(screen.getByText("LogoutButton")).toBeInTheDocument();

    expect(screen.getAllByText(/[¥￥]800,000/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/[¥￥]620,000/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/[¥￥]180,000/).length).toBeGreaterThan(0);

    expect(screen.getAllByText("PAY-012").length).toBeGreaterThan(0);
    expect(screen.getAllByText("PAY-013").length).toBeGreaterThan(0);

    expect(screen.getAllByText("株式会社A").length).toBeGreaterThan(0);
    expect(screen.getAllByText("株式会社B").length).toBeGreaterThan(0);

    expect(screen.getAllByText("一部割当").length).toBeGreaterThan(0);
    expect(screen.getAllByText("割当済").length).toBeGreaterThan(0);

    expect(screen.getAllByText(/[¥￥]300,000/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/[¥￥]120,000/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/[¥￥]180,000/).length).toBeGreaterThan(0);

    expect(screen.getAllByText("INV-0010").length).toBeGreaterThan(0);
    expect(screen.getAllByText("INV-0011").length).toBeGreaterThan(0);
    expect(screen.getAllByText("INV-0020").length).toBeGreaterThan(0);

    expect(screen.getByRole("link", { name: "← 管理者トップへ" })).toHaveAttribute(
      "href",
      "/dashboards/admin"
    );
    expect(screen.getByRole("link", { name: "+ 入金登録" })).toHaveAttribute(
      "href",
      "/payments/new"
    );

expect(fetchMock).toHaveBeenCalledWith(
  expect.stringContaining("/api/payments?year=2026"),
  expect.objectContaining({
    method: "GET",
    cache: "no-store",
    headers: {
      Authorization: "Bearer dummy-token",
    },
  })
);

expect(fetchMock.mock.calls[0][0]).toContain("q=%E6%A0%AA%E5%BC%8F%E4%BC%9A%E7%A4%BE");
expect(fetchMock.mock.calls[0][0]).toContain("status=all");
expect(fetchMock.mock.calls[0][0]).toContain("page=1");
expect(fetchMock.mock.calls[0][0]).toContain("pageSize=10");
  });

  it("0件のとき空状態メッセージとページ表示を出す", async () => {
    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        year: 2026,
        month: "all",
        keyword: "",
        status: "all",
        page: 1,
        pageSize: 10,
        totalCount: 0,
        rows: [],
        summary: {
          totalAmount: 0,
          allocatedTotal: 0,
          unallocatedTotal: 0,
        },
      }),
      status: 200,
      statusText: "OK",
    });

    const element = await PaymentsPage({
      searchParams: Promise.resolve({
        page: "1",
      }),
    });

    render(element);

    expect(
      screen.getAllByText("条件に一致するデータがありません。").length
    ).toBeGreaterThan(0);

    expect(screen.getByText("入金一覧（0件）")).toBeInTheDocument();
    expect(screen.getByText("1 / 1 ページ")).toBeInTheDocument();
  });

  it("前へ・次へリンクに検索条件を引き継ぐ", async () => {
    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        year: 2026,
        month: "3",
        keyword: "INV",
        status: "PARTIAL",
        page: 2,
        pageSize: 10,
        totalCount: 25,
        rows: [
          {
            id: 30,
            paymentDate: "2026-03-10T00:00:00",
            payerName: "株式会社C",
            amount: 100000,
            allocatedAmount: 50000,
            invoices: [{ id: 30, invoiceNumber: "INV-0030" }],
            status: "PARTIAL",
          },
        ],
        summary: {
          totalAmount: 1000000,
          allocatedTotal: 700000,
          unallocatedTotal: 300000,
        },
      }),
      status: 200,
      statusText: "OK",
    });

    const element = await PaymentsPage({
      searchParams: Promise.resolve({
        year: "2026",
        month: "3",
        q: "INV",
        status: "partial",
        page: "2",
      }),
    });

    render(element);

    expect(screen.getByText("2 / 3 ページ")).toBeInTheDocument();

    expect(screen.getByRole("link", { name: "← 前へ" })).toHaveAttribute(
      "href",
      "/payments?year=2026&month=3&status=partial&q=INV&page=1"
    );

    expect(screen.getByRole("link", { name: "次へ →" })).toHaveAttribute(
      "href",
      "/payments?year=2026&month=3&status=partial&q=INV&page=3"
    );
  });

it("UNALLOCATED / PARTIAL / ALLOCATED を表示分岐できる", async () => {
  fetchMock.mockResolvedValueOnce({
    ok: true,
    json: async () => ({
      year: 2026,
      month: "all",
      keyword: "",
      status: "all",
      page: 1,
      pageSize: 10,
      totalCount: 3,
      rows: [
        {
          id: 1,
          paymentDate: "2026-03-01T00:00:00",
          payerName: "株式会社未割当",
          amount: 100000,
          allocatedAmount: 0,
          invoices: [],
          status: "UNALLOCATED",
        },
        {
          id: 2,
          paymentDate: "2026-03-02T00:00:00",
          payerName: "株式会社一部",
          amount: 200000,
          allocatedAmount: 50000,
          invoices: [{ id: 21, invoiceNumber: "INV-0021" }],
          status: "PARTIAL",
        },
        {
          id: 3,
          paymentDate: "2026-03-03T00:00:00",
          payerName: "株式会社完了",
          amount: 300000,
          allocatedAmount: 300000,
          invoices: [{ id: 31, invoiceNumber: "INV-0031" }],
          status: "ALLOCATED",
        },
      ],
      summary: {
        totalAmount: 600000,
        allocatedTotal: 350000,
        unallocatedTotal: 250000,
      },
    }),
    status: 200,
    statusText: "OK",
  });

  const element = await PaymentsPage({
    searchParams: Promise.resolve({
      year: "2026",
      month: "all",
      status: "all",
      page: "1",
    }),
  });

  render(element);

  expect(screen.getAllByText("未割当").length).toBeGreaterThan(0);
  expect(screen.getAllByText("一部割当").length).toBeGreaterThan(0);
  expect(screen.getAllByText("割当済").length).toBeGreaterThan(0);
});
  
it("month=all のとき month を付けず、month=3 のとき month=3 で取得する", async () => {
  fetchMock.mockResolvedValueOnce({
    ok: true,
    json: async () => ({
      year: 2026,
      month: "all",
      keyword: "",
      status: "all",
      page: 1,
      pageSize: 10,
      totalCount: 0,
      rows: [],
      summary: {
        totalAmount: 0,
        allocatedTotal: 0,
        unallocatedTotal: 0,
      },
    }),
    status: 200,
    statusText: "OK",
  });

  await PaymentsPage({
    searchParams: Promise.resolve({
      year: "2026",
      month: "all",
      status: "all",
      page: "1",
    }),
  });

  expect(String(fetchMock.mock.calls[0][0])).toContain("year=2026");
  expect(String(fetchMock.mock.calls[0][0])).toContain("status=all");
  expect(String(fetchMock.mock.calls[0][0])).not.toContain("month=");

  jest.clearAllMocks();

  fetchMock.mockResolvedValueOnce({
    ok: true,
    json: async () => ({
      year: 2026,
      month: "3",
      keyword: "",
      status: "all",
      page: 1,
      pageSize: 10,
      totalCount: 0,
      rows: [],
      summary: {
        totalAmount: 0,
        allocatedTotal: 0,
        unallocatedTotal: 0,
      },
    }),
    status: 200,
    statusText: "OK",
  });

  await PaymentsPage({
    searchParams: Promise.resolve({
      year: "2026",
      month: "3",
      status: "all",
      page: "1",
    }),
  });

  expect(String(fetchMock.mock.calls[0][0])).toContain("month=3");
});

it("payerName が null のとき null を表示しない", async () => {
  fetchMock.mockResolvedValueOnce({
    ok: true,
    json: async () => ({
      year: 2026,
      month: "all",
      keyword: "",
      status: "all",
      page: 1,
      pageSize: 10,
      totalCount: 1,
      rows: [
        {
          id: 50,
          paymentDate: "2026-03-20T00:00:00",
          payerName: null,
          amount: 120000,
          allocatedAmount: 0,
          invoices: [],
          status: "UNALLOCATED",
        },
      ],
      summary: {
        totalAmount: 120000,
        allocatedTotal: 0,
        unallocatedTotal: 120000,
      },
    }),
    status: 200,
    statusText: "OK",
  });

  const element = await PaymentsPage({
    searchParams: Promise.resolve({
      year: "2026",
      month: "all",
      status: "all",
      page: "1",
    }),
  });

  const { container } = render(element);

  expect(screen.getAllByText("PAY-050").length).toBeGreaterThan(0);
  expect(container.textContent).not.toContain("null");
});

it("token がないときエラーになる", async () => {
  mockCookies.mockResolvedValue({
    get: () => undefined,
    getAll: () => [],
  } as any);

  await expect(
    PaymentsPage({
      searchParams: Promise.resolve({
        year: "2026",
        month: "all",
        status: "all",
        page: "1",
      }),
    })
  ).rejects.toThrow(/token cookie missing/i);

  expect(fetchMock).not.toHaveBeenCalled();
});

});