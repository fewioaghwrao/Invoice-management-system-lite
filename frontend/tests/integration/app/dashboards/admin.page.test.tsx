import { render, screen } from "@testing-library/react";
import AdminDashboardPage from "@/app/dashboards/admin/page";
import { apiGetServer } from "@/lib/api.server";

jest.mock("@/lib/api.server", () => ({
  apiGetServer: jest.fn(),
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

jest.mock("@/components/MonthlySalesChartClient", () => {
  return function MockMonthlySalesChartClient(props: any) {
    return (
      <div>
        MonthlySalesChartClient
        <span>{props.year}</span>
      </div>
    );
  };
});

const mockApiGetServer = apiGetServer as jest.MockedFunction<typeof apiGetServer>;

describe("AdminDashboardPage", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("管理者ダッシュボードの主要情報を表示する", async () => {
    mockApiGetServer
      .mockResolvedValueOnce({
        year: 2026,
        invoiceTotal: 1200000,
        paidTotal: 900000,
        remainingTotal: 300000,
        recoveryRate: 75,
        invoiceCount: 12,
        paymentCount: 8,
        monthlySales: [
          { month: 1, invoiceTotal: 300000 },
          { month: 2, invoiceTotal: 400000 },
        ],
        unpaidTop5: [
          {
            invoiceId: 10,
            invoiceNumber: "INV-0010",
            clientName: "株式会社A",
            dueDate: "2026-03-01",
            invoiceTotal: 500000,
            paidTotal: 200000,
            remainingTotal: 300000,
            isOverdue: true,
          },
        ],
      } as any)
      .mockResolvedValueOnce({
        year: 2026,
        month: "all",
        keyword: "",
        count: 1,
        rows: [
          {
            memberId: 1,
            memberName: "会員A",
            invoiceTotal: 1000000,
            paidTotal: 500000,
            remainingTotal: 500000,
            recoveryRate: 50,
          },
        ],
      } as any)
      .mockResolvedValueOnce([
        {
          id: 1,
          at: "2026-03-23T10:00:00+09:00",
          actorUserId: 99,
          action: "PAYMENT_ALLOCATION_ADDED",
          entity: "PAYMENT",
          entityId: "15",
          summary: "入金割当を追加しました",
        },
      ] as any);

    const element = await AdminDashboardPage({
      searchParams: Promise.resolve({ year: "2026" }),
    });

    render(element);

    expect(
      screen.getByText("請求・入金ステータスダッシュボード（管理者）")
    ).toBeInTheDocument();

expect(screen.getByText(/[¥￥]1,200,000/)).toBeInTheDocument();
expect(screen.getAllByText(/[¥￥]300,000/).length).toBeGreaterThan(0);
expect(screen.getByText("12")).toBeInTheDocument();
expect(screen.getByText("8")).toBeInTheDocument();
expect(screen.getByText("75.0%")).toBeInTheDocument();

expect(screen.getAllByText("INV-0010").length).toBeGreaterThan(0);
expect(screen.getAllByText("株式会社A").length).toBeGreaterThan(0);

    expect(screen.getByText("会員A")).toBeInTheDocument();
    expect(screen.getByText("50.0%")).toBeInTheDocument();

expect(screen.getAllByText("割当追加").length).toBeGreaterThan(0);
expect(screen.getAllByText("入金 #15").length).toBeGreaterThan(0);
expect(screen.getAllByText("入金割当を追加しました").length).toBeGreaterThan(0);

    expect(screen.getByText("CurrentUserBadge")).toBeInTheDocument();
    expect(screen.getByText("LogoutButton")).toBeInTheDocument();
    expect(screen.getByText("MonthlySalesChartClient")).toBeInTheDocument();
  });

  it("未入金・ワースト顧客・操作ログが空のとき空状態メッセージを表示する", async () => {
    mockApiGetServer
      .mockResolvedValueOnce({
        year: 2026,
        invoiceTotal: 0,
        paidTotal: 0,
        remainingTotal: 0,
        recoveryRate: 0,
        invoiceCount: 0,
        paymentCount: 0,
        monthlySales: [],
        unpaidTop5: [],
      } as any)
      .mockResolvedValueOnce({
        year: 2026,
        month: "all",
        keyword: "",
        count: 0,
        rows: [],
      } as any)
      .mockResolvedValueOnce([] as any);

    const element = await AdminDashboardPage({
      searchParams: Promise.resolve({ year: "2026" }),
    });

    render(element);

expect(
  screen.getAllByText("現在、未入金の請求書はありません。").length
).toBeGreaterThan(0);

expect(
  screen.getByText("未回収がある顧客がありません（回収率ワースト対象なし）")
).toBeInTheDocument();

expect(
  screen.getAllByText("最近の操作ログはありません。").length
).toBeGreaterThan(0);
  });

  it("年切替リンクを表示する", async () => {
    mockApiGetServer
      .mockResolvedValueOnce({
        year: 2026,
        invoiceTotal: 1000,
        paidTotal: 1000,
        remainingTotal: 0,
        recoveryRate: 100,
        invoiceCount: 1,
        paymentCount: 1,
        monthlySales: [],
        unpaidTop5: [],
      } as any)
      .mockResolvedValueOnce({
        year: 2026,
        month: "all",
        keyword: "",
        count: 0,
        rows: [],
      } as any)
      .mockResolvedValueOnce([] as any);

    const element = await AdminDashboardPage({
      searchParams: Promise.resolve({ year: "2026" }),
    });

    render(element);

    expect(screen.getByRole("link", { name: "前年へ" })).toHaveAttribute(
      "href",
      "/dashboards/admin?year=2025"
    );

    expect(screen.getByRole("link", { name: "翌年へ" })).toHaveAttribute(
      "href",
      "/dashboards/admin?year=2027"
    );
  });

it("year 未指定時は当年で API を呼ぶ", async () => {
  const currentYear = new Date().getFullYear();

  mockApiGetServer
    .mockResolvedValueOnce({
      year: currentYear,
      invoiceTotal: 0,
      paidTotal: 0,
      remainingTotal: 0,
      recoveryRate: 0,
      invoiceCount: 0,
      paymentCount: 0,
      monthlySales: [],
      unpaidTop5: [],
    } as any)
    .mockResolvedValueOnce({
      year: currentYear,
      month: "all",
      keyword: "",
      count: 0,
      rows: [],
    } as any)
    .mockResolvedValueOnce([] as any);

  const element = await AdminDashboardPage({
    searchParams: Promise.resolve({}),
  });

  render(element);

  expect(mockApiGetServer).toHaveBeenNthCalledWith(
    1,
    `/api/admin/summary?year=${currentYear}`
  );
  expect(mockApiGetServer).toHaveBeenNthCalledWith(
    2,
    `/api/sales/by-member/worst-top5?year=${currentYear}`
  );
  expect(mockApiGetServer).toHaveBeenNthCalledWith(
    3,
    `/api/admin/operation-logs/recent?limit=5`
  );
});

it("year が不正値でも当年で API を呼ぶ", async () => {
  const currentYear = new Date().getFullYear();

  mockApiGetServer
    .mockResolvedValueOnce({
      year: currentYear,
      invoiceTotal: 0,
      paidTotal: 0,
      remainingTotal: 0,
      recoveryRate: 0,
      invoiceCount: 0,
      paymentCount: 0,
      monthlySales: [],
      unpaidTop5: [],
    } as any)
    .mockResolvedValueOnce({
      year: currentYear,
      month: "all",
      keyword: "",
      count: 0,
      rows: [],
    } as any)
    .mockResolvedValueOnce([] as any);

  const element = await AdminDashboardPage({
    searchParams: Promise.resolve({ year: "abc" }),
  });

  render(element);

  expect(mockApiGetServer).toHaveBeenNthCalledWith(
    1,
    `/api/admin/summary?year=${currentYear}`
  );
  expect(mockApiGetServer).toHaveBeenNthCalledWith(
    2,
    `/api/sales/by-member/worst-top5?year=${currentYear}`
  );
  expect(mockApiGetServer).toHaveBeenNthCalledWith(
    3,
    `/api/admin/operation-logs/recent?limit=5`
  );
});
it("操作ログの action / entity 分岐を表示する", async () => {
  mockApiGetServer
    .mockResolvedValueOnce({
      year: 2026,
      invoiceTotal: 0,
      paidTotal: 0,
      remainingTotal: 0,
      recoveryRate: 0,
      invoiceCount: 0,
      paymentCount: 0,
      monthlySales: [],
      unpaidTop5: [],
    } as any)
    .mockResolvedValueOnce({
      year: 2026,
      month: "all",
      keyword: "",
      count: 0,
      rows: [],
    } as any)
    .mockResolvedValueOnce([
      {
        id: 1,
        at: "2026-03-23T10:00:00+09:00",
        actorUserId: 99,
        action: "PAYMENT_ALLOCATION_DELETED",
        entity: "PAYMENT",
        entityId: "15",
        summary: "割当削除",
      },
      {
        id: 2,
        at: "2026-03-23T10:10:00+09:00",
        actorUserId: 99,
        action: "PAYMENT_ALLOCATIONS_REPLACED",
        entity: "INVOICE",
        entityId: "22",
        summary: "割当置換",
      },
      {
        id: 3,
        at: "2026-03-23T10:20:00+09:00",
        actorUserId: 99,
        action: "PAYMENT_ALLOCATIONS_CLEARED",
        entity: "MEMBER",
        entityId: "8",
        summary: "割当クリア",
      },
      {
        id: 4,
        at: "2026-03-23T10:30:00+09:00",
        actorUserId: 99,
        action: "UNKNOWN_ACTION",
        entity: "UNKNOWN",
        entityId: "",
        summary: "不明な操作",
      },
    ] as any);

  const element = await AdminDashboardPage({
    searchParams: Promise.resolve({ year: "2026" }),
  });

  render(element);

  expect(screen.getAllByText("割当削除").length).toBeGreaterThan(0);
  expect(screen.getAllByText("割当置換").length).toBeGreaterThan(0);
  expect(screen.getAllByText("割当クリア").length).toBeGreaterThan(0);
  expect(screen.getAllByText("不明な操作").length).toBeGreaterThan(0);
});

it("期限超過表示と下段リンクを表示する", async () => {
  mockApiGetServer
    .mockResolvedValueOnce({
      year: 2026,
      invoiceTotal: 1000,
      paidTotal: 0,
      remainingTotal: 1000,
      recoveryRate: 0,
      invoiceCount: 1,
      paymentCount: 0,
      monthlySales: [],
      unpaidTop5: [
        {
          invoiceId: 10,
          invoiceNumber: "INV-0010",
          clientName: "株式会社A",
          dueDate: "2026-03-01",
          invoiceTotal: 500000,
          paidTotal: 200000,
          remainingTotal: 300000,
          isOverdue: true,
        },
      ],
    } as any)
    .mockResolvedValueOnce({
      year: 2026,
      month: "all",
      keyword: "",
      count: 0,
      rows: [],
    } as any)
    .mockResolvedValueOnce([] as any);

  const element = await AdminDashboardPage({
    searchParams: Promise.resolve({ year: "2026" }),
  });

  render(element);

  expect(screen.getAllByText("期限超過").length).toBeGreaterThan(0);

  expect(screen.getByRole("link", { name: /請求書一覧/ })).toHaveAttribute("href", "/invoices");
  expect(screen.getByRole("link", { name: /会員一覧/ })).toHaveAttribute("href", "/members");
  expect(screen.getByRole("link", { name: /売上一覧/ })).toHaveAttribute("href", "/sales");
  expect(screen.getByRole("link", { name: /入金一覧/ })).toHaveAttribute("href", "/payments");
});

it("操作ログで summary なし・entity 不明・entityId なしのフォールバックを表示する", async () => {
  mockApiGetServer
    .mockResolvedValueOnce({
      year: 2026,
      invoiceTotal: 0,
      paidTotal: 0,
      remainingTotal: 0,
      recoveryRate: 0,
      invoiceCount: 0,
      paymentCount: 0,
      monthlySales: [],
      unpaidTop5: [],
    } as any)
    .mockResolvedValueOnce({
      year: 2026,
      month: "all",
      keyword: "",
      count: 0,
      rows: [],
    } as any)
    .mockResolvedValueOnce([
      {
        id: 1,
        at: "2026-03-23T10:30:00+09:00",
        actorUserId: 99,
        action: "UNKNOWN_ACTION",
        entity: "UNKNOWN",
        entityId: "",
        summary: "",
      },
    ] as any);

  const element = await AdminDashboardPage({
    searchParams: Promise.resolve({ year: "2026" }),
  });

  render(element);

  expect(screen.getAllByText(/UNKNOWN_ACTION|不明な操作/).length).toBeGreaterThan(0);
});

it("最近の操作ログが空のとき空メッセージを表示する", async () => {
  mockApiGetServer
    .mockResolvedValueOnce({
      year: 2026,
      invoiceTotal: 0,
      paidTotal: 0,
      remainingTotal: 0,
      recoveryRate: 0,
      invoiceCount: 0,
      paymentCount: 0,
      monthlySales: [],
      unpaidTop5: [],
    } as any)
    .mockResolvedValueOnce({
      year: 2026,
      month: "all",
      keyword: "",
      count: 0,
      rows: [],
    } as any)
    .mockResolvedValueOnce([] as any);

  const element = await AdminDashboardPage({
    searchParams: Promise.resolve({ year: "2026" }),
  });

  render(element);

  expect(
    screen.getAllByText("最近の操作ログはありません。").length
  ).toBeGreaterThan(0);
});

it("未入金上位が空のとき売上ワースト会員セクションが空表示になる", async () => {
  mockApiGetServer
    .mockResolvedValueOnce({
      year: 2026,
      invoiceTotal: 0,
      paidTotal: 0,
      remainingTotal: 0,
      recoveryRate: 0,
      invoiceCount: 0,
      paymentCount: 0,
      monthlySales: [],
      unpaidTop5: [],
    } as any)
    .mockResolvedValueOnce({
      year: 2026,
      month: "all",
      keyword: "",
      count: 0,
      rows: [],
    } as any)
    .mockResolvedValueOnce([
      {
        id: 1,
        at: "2026-03-23T10:00:00+09:00",
        actorUserId: 99,
        action: "PAYMENT_ALLOCATION_DELETED",
        entity: "PAYMENT",
        entityId: "15",
        summary: "割当削除",
      },
    ] as any);

  const element = await AdminDashboardPage({
    searchParams: Promise.resolve({ year: "2026" }),
  });

  render(element);

  expect(
    screen.queryByText("株式会社A")
  ).not.toBeInTheDocument();
});

it("操作ログで 割当追加 と entityId なしの対象表示を処理できる", async () => {
  mockApiGetServer
    .mockResolvedValueOnce({
      year: 2026,
      invoiceTotal: 0,
      paidTotal: 0,
      remainingTotal: 0,
      recoveryRate: 0,
      invoiceCount: 0,
      paymentCount: 0,
      monthlySales: [],
      unpaidTop5: [],
    } as any)
    .mockResolvedValueOnce({
      year: 2026,
      month: "all",
      keyword: "",
      count: 0,
      rows: [],
    } as any)
    .mockResolvedValueOnce([
      {
        id: 1,
        at: "2026-03-23T10:00:00+09:00",
        actorUserId: 99,
        action: "PAYMENT_ALLOCATION_ADDED",
        entity: "PAYMENT",
        entityId: "",
        summary: "追加しました",
      },
    ] as any);

  render(
    await AdminDashboardPage({
      searchParams: Promise.resolve({ year: "2026" }),
    })
  );

  expect(screen.getAllByText("割当追加").length).toBeGreaterThan(0);
  expect(screen.getAllByText("入金").length).toBeGreaterThan(0);
});

it("summary DTO の monthlySales / unpaidTop5 が未設定でも表示できる", async () => {
  mockApiGetServer
    .mockResolvedValueOnce({
      year: 2026,
      invoiceTotal: 0,
      paidTotal: 0,
      remainingTotal: 0,
      recoveryRate: 0,
      invoiceCount: 0,
      paymentCount: 0,
      monthlySales: undefined,
      unpaidTop5: undefined,
    } as any)
    .mockResolvedValueOnce({
      year: 2026,
      month: "all",
      keyword: "",
      count: 0,
      rows: [],
    } as any)
    .mockResolvedValueOnce([] as any);

  render(
    await AdminDashboardPage({
      searchParams: Promise.resolve({ year: "2026" }),
    })
  );

  expect(screen.getByText("請求・入金ステータスダッシュボード（管理者）")).toBeInTheDocument();
});

it("操作ログで action / target の未通過分岐をまとめて表示できる", async () => {
  mockApiGetServer
    .mockResolvedValueOnce({
      year: 2026,
      invoiceTotal: 0,
      paidTotal: 0,
      remainingTotal: 0,
      recoveryRate: 0,
      invoiceCount: 0,
      paymentCount: 0,
      monthlySales: [],
      unpaidTop5: [],
    } as any)
    .mockResolvedValueOnce({
      year: 2026,
      month: "all",
      keyword: "",
      count: 0,
      rows: [],
    } as any)
    .mockResolvedValueOnce([
      {
        id: 1,
        at: "2026-03-23T10:00:00+09:00",
        actorUserId: 99,
        action: "PAYMENT_ALLOCATION_ADDED",
        entity: "PAYMENT",
        entityId: "",
        summary: "割当追加",
      },
      {
        id: 2,
        at: "2026-03-23T10:10:00+09:00",
        actorUserId: 99,
        action: "UNKNOWN_ACTION",
        entity: "UNKNOWN_ENTITY",
        entityId: "",
        summary: "",
      },
    ] as any);

  const element = await AdminDashboardPage({
    searchParams: Promise.resolve({ year: "2026" }),
  });

  render(element);

  expect(screen.getAllByText("割当追加").length).toBeGreaterThan(0);
  expect(screen.getAllByText("入金").length).toBeGreaterThan(0);

  expect(
    screen.getAllByText(/UNKNOWN_ACTION|不明な操作/).length
  ).toBeGreaterThan(0);
  expect(screen.getAllByText("UNKNOWN_ENTITY").length).toBeGreaterThan(0);
});

it("summary DTO の欠損値をフォールバックして表示できる", async () => {
  mockApiGetServer
    .mockResolvedValueOnce({
      year: 2026,
      invoiceTotal: 0,
      paidTotal: 0,
      remainingTotal: 0,
      recoveryRate: 0,
      invoiceCount: 0,
      paymentCount: 0,
      monthlySales: [
        {
          month: 4,
          invoiceTotal: undefined,
        },
      ],
      unpaidTop5: [
        {
          invoiceId: 10,
          invoiceNumber: "INV-0010",
          clientName: "株式会社A",
          dueDate: "2026-03-01",
          invoiceTotal: 500000,
          paidTotal: 500000,
          remainingTotal: undefined,
          isOverdue: undefined,
        },
      ],
    } as any)
    .mockResolvedValueOnce({
      year: 2026,
      month: "all",
      keyword: "",
      count: 0,
      rows: [],
    } as any)
    .mockResolvedValueOnce([] as any);

  const element = await AdminDashboardPage({
    searchParams: Promise.resolve({ year: "" }),
  });

  render(element);

  expect(
    screen.getByText("請求・入金ステータスダッシュボード（管理者）")
  ).toBeInTheDocument();

  expect(screen.queryByText("期限超過")).not.toBeInTheDocument();
});

});