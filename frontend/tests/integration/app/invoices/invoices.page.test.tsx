import { render, screen } from "@testing-library/react";
import InvoicesPage from "@/app/invoices/page";
import { apiGetServer } from "@/lib/api.server";

jest.mock("@/lib/api.server", () => ({
  apiGetServer: jest.fn(),
}));

const mockApiGetServer = apiGetServer as jest.MockedFunction<typeof apiGetServer>;

describe("InvoicesPage", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("請求書一覧を表示し、検索条件付き詳細リンクと督促リンクを出す", async () => {
    mockApiGetServer.mockResolvedValueOnce([
      {
        id: 10,
        invoiceNumber: "INV-0010",
        memberName: "株式会社A",
        invoiceDate: "2026-03-01T00:00:00",
        dueDate: "2026-03-31T00:00:00",
        totalAmount: 300000,
        statusName: "未入金",
      },
      {
        id: 11,
        invoiceNumber: "INV-0011",
        memberName: "株式会社B",
        invoiceDate: "2026-03-05T00:00:00",
        dueDate: "2026-04-04T00:00:00",
        totalAmount: 120000,
        statusName: "一部入金",
      },
    ] as any);

    const element = await InvoicesPage({
      searchParams: Promise.resolve({
        invoiceNumber: "INV",
        memberName: "株式会社",
        statusId: "1",
        fromInvoiceDate: "2026-03-01",
        toInvoiceDate: "2026-03-31",
        page: "1",
      }),
    });

    render(element);

    expect(screen.getByText("請求書一覧（管理者）")).toBeInTheDocument();
    expect(
      screen.getByText(
        "請求番号・会員名・ステータス・請求日で検索し、入金状況や支払期限を一覧で確認できます。"
      )
    ).toBeInTheDocument();

    expect(screen.getAllByText("INV-0010").length).toBeGreaterThan(0);
    expect(screen.getAllByText("株式会社A").length).toBeGreaterThan(0);
    expect(screen.getAllByText(/[¥￥]300,000/).length).toBeGreaterThan(0);
    expect(screen.getAllByText("未入金").length).toBeGreaterThan(0);

    expect(screen.getAllByText("INV-0011").length).toBeGreaterThan(0);
    expect(screen.getAllByText("株式会社B").length).toBeGreaterThan(0);
    expect(screen.getAllByText(/[¥￥]120,000/).length).toBeGreaterThan(0);
    expect(screen.getAllByText("一部入金").length).toBeGreaterThan(0);

    expect(mockApiGetServer).toHaveBeenCalledWith("/api/invoices", {
      InvoiceNumber: "INV",
      MemberName: "株式会社",
      StatusId: "1",
      FromInvoiceDate: "2026-03-01",
      ToInvoiceDate: "2026-03-31",
      Page: 1,
      PageSize: 5,
    });

    const detailLinks = screen.getAllByRole("link", { name: /INV-0010|請求書詳細|詳細/ });
    expect(detailLinks.length).toBeGreaterThan(0);

    expect(screen.getAllByRole("link", { name: "督促" }).length).toBeGreaterThan(0);

    expect(screen.getByRole("link", { name: "＋ 新規作成" })).toHaveAttribute(
      "href",
      "/invoices/new"
    );
  });

  it("請求書が0件のとき空状態と0件表示を出す", async () => {
    mockApiGetServer.mockResolvedValueOnce([] as any);

    const element = await InvoicesPage({
      searchParams: Promise.resolve({
        page: "1",
      }),
    });

    render(element);

    expect(
      screen.getAllByText("該当する請求書はありません。").length
    ).toBeGreaterThan(0);

    expect(screen.getByText("0件")).toBeInTheDocument();

    expect(screen.getByRole("link", { name: "前へ" })).toHaveAttribute("href", "#");
    expect(screen.getByRole("link", { name: "次へ" })).toHaveAttribute("href", "#");
  });

  it("次へ・前へリンクに検索条件を引き継ぐ", async () => {
    mockApiGetServer.mockResolvedValueOnce([
      {
        id: 20,
        invoiceNumber: "INV-0020",
        memberName: "株式会社C",
        invoiceDate: "2026-02-01T00:00:00",
        dueDate: "2026-02-28T00:00:00",
        totalAmount: 50000,
        statusName: "入金済み",
      },
      {
        id: 21,
        invoiceNumber: "INV-0021",
        memberName: "株式会社D",
        invoiceDate: "2026-02-02T00:00:00",
        dueDate: "2026-03-01T00:00:00",
        totalAmount: 60000,
        statusName: "入金済み",
      },
      {
        id: 22,
        invoiceNumber: "INV-0022",
        memberName: "株式会社E",
        invoiceDate: "2026-02-03T00:00:00",
        dueDate: "2026-03-02T00:00:00",
        totalAmount: 70000,
        statusName: "入金済み",
      },
      {
        id: 23,
        invoiceNumber: "INV-0023",
        memberName: "株式会社F",
        invoiceDate: "2026-02-04T00:00:00",
        dueDate: "2026-03-03T00:00:00",
        totalAmount: 80000,
        statusName: "入金済み",
      },
      {
        id: 24,
        invoiceNumber: "INV-0024",
        memberName: "株式会社G",
        invoiceDate: "2026-02-05T00:00:00",
        dueDate: "2026-03-04T00:00:00",
        totalAmount: 90000,
        statusName: "入金済み",
      },
    ] as any);

    const element = await InvoicesPage({
      searchParams: Promise.resolve({
        invoiceNumber: "FIX",
        memberName: "株式会社",
        statusId: "3",
        fromInvoiceDate: "2026-02-01",
        toInvoiceDate: "2026-02-28",
        page: "2",
      }),
    });

    render(element);

    expect(
      screen.getByText("6–10件を表示（1ページあたり 5件）")
    ).toBeInTheDocument();

    expect(screen.getByRole("link", { name: "前へ" })).toHaveAttribute(
      "href",
      "/invoices?invoiceNumber=FIX&memberName=%E6%A0%AA%E5%BC%8F%E4%BC%9A%E7%A4%BE&statusId=3&fromInvoiceDate=2026-02-01&toInvoiceDate=2026-02-28&page=1"
    );

    expect(screen.getByRole("link", { name: "次へ" })).toHaveAttribute(
      "href",
      "/invoices?invoiceNumber=FIX&memberName=%E6%A0%AA%E5%BC%8F%E4%BC%9A%E7%A4%BE&statusId=3&fromInvoiceDate=2026-02-01&toInvoiceDate=2026-02-28&page=3"
    );
  });
});