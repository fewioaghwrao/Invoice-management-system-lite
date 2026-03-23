import { render, screen } from "@testing-library/react";
import InvoiceDetailPage from "@/app/invoices/[id]/page";
import { apiGetServer } from "@/lib/api.server";

jest.mock("@/lib/api.server", () => ({
  apiGetServer: jest.fn(),
}));

jest.mock("@/app/invoices/[id]/DeleteInvoiceButton", () => {
  return function MockDeleteInvoiceButton(props: { invoiceId: string }) {
    return <button>DeleteInvoiceButton:{props.invoiceId}</button>;
  };
});

jest.mock("@/app/invoices/[id]/PdfDownloadButton", () => {
  return function MockPdfDownloadButton(props: { invoiceId: number }) {
    return <button>PdfDownloadButton:{props.invoiceId}</button>;
  };
});

const mockApiGetServer = apiGetServer as jest.MockedFunction<typeof apiGetServer>;

describe("InvoiceDetailPage", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("請求書詳細の主要情報を表示する", async () => {
    mockApiGetServer.mockResolvedValueOnce({
      id: 10,
      invoiceNumber: "INV-0010",
      statusName: "一部入金",
      totalAmount: 300000,
      paidAmount: 120000,
      remainingAmount: 180000,
      invoiceDate: "2026-03-01T00:00:00",
      dueDate: "2026-03-31T00:00:00",
      memberName: "株式会社A",
      remarks: "3月分の請求です",
      reminders: [
        {
          id: 1,
          remindedAt: "2026-03-20T10:00:00",
          method: "メール",
          note: "初回督促",
        },
      ],
      allocations: [
        {
          paymentId: 100,
          paymentDate: "2026-03-15T00:00:00",
          method: "BANK_TRANSFER",
          payerName: "株式会社A",
          importBatchId: 55,
          allocatedAmount: 120000,
        },
      ],
    } as any);

    const element = await InvoiceDetailPage({
      params: Promise.resolve({ id: "10" }),
      searchParams: Promise.resolve({
        from: "invoiceNumber=INV&statusId=1&page=2",
      }),
    });

    render(element);

expect(screen.getAllByText("INV-0010").length).toBeGreaterThan(0);
    expect(screen.getByText("一部入金")).toBeInTheDocument();
    expect(
      screen.getByText("請求情報・入金割当・督促履歴・残額をまとめて確認できます。")
    ).toBeInTheDocument();

    expect(screen.getAllByText(/[¥￥]300,000/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/[¥￥]120,000/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/[¥￥]180,000/).length).toBeGreaterThan(0);

    expect(screen.getByText("2026-03-01")).toBeInTheDocument();
    expect(screen.getByText("2026-03-31")).toBeInTheDocument();
    expect(screen.getAllByText("株式会社A").length).toBeGreaterThan(0);
    expect(screen.getByText("3月分の請求です")).toBeInTheDocument();

    expect(screen.getByText("2026-03-20 / メール")).toBeInTheDocument();
    expect(screen.getByText("初回督促")).toBeInTheDocument();

    expect(screen.getByText("2026-03-15 / 銀行振込")).toBeInTheDocument();
    expect(screen.getByText("名義：株式会社A / 取込：#55")).toBeInTheDocument();
    expect(screen.getAllByText(/[¥￥]120,000/).length).toBeGreaterThan(0);

    expect(screen.getByText("DeleteInvoiceButton:10")).toBeInTheDocument();
    expect(screen.getByText("PdfDownloadButton:10")).toBeInTheDocument();

    expect(mockApiGetServer).toHaveBeenCalledWith("/api/invoices/10");
  });

  it("督促履歴と入金履歴が空のとき空状態メッセージを表示する", async () => {
    mockApiGetServer.mockResolvedValueOnce({
      id: 11,
      invoiceNumber: "INV-0011",
      statusName: "未入金",
      totalAmount: 50000,
      paidAmount: 0,
      remainingAmount: 50000,
      invoiceDate: "2026-04-01T00:00:00",
      dueDate: "2026-04-30T00:00:00",
      memberName: "株式会社B",
      remarks: null,
      reminders: [],
      allocations: [],
    } as any);

    const element = await InvoiceDetailPage({
      params: Promise.resolve({ id: "11" }),
      searchParams: Promise.resolve({}),
    });

    render(element);

   expect(screen.getAllByText("INV-0011").length).toBeGreaterThan(0);
    expect(screen.getByText("未入金")).toBeInTheDocument();
    expect(screen.getByText("—")).toBeInTheDocument();

    expect(
      screen.getByText("督促履歴がありません。")
    ).toBeInTheDocument();

    expect(
      screen.getByText("入金履歴がありません。")
    ).toBeInTheDocument();
  });

  it("from があると一覧・編集・入金登録リンクに引き継ぐ", async () => {
    mockApiGetServer.mockResolvedValueOnce({
      id: 12,
      invoiceNumber: "INV-0012",
      statusName: "入金済み",
      totalAmount: 10000,
      paidAmount: 10000,
      remainingAmount: 0,
      invoiceDate: "2026-05-01T00:00:00",
      dueDate: "2026-05-31T00:00:00",
      memberName: "株式会社C",
      remarks: null,
      reminders: [],
      allocations: [],
    } as any);

    const from = "invoiceNumber=FIX&memberName=%E6%A0%AA%E5%BC%8F&page=3";

    const element = await InvoiceDetailPage({
      params: Promise.resolve({ id: "12" }),
      searchParams: Promise.resolve({ from }),
    });

    render(element);

    expect(screen.getByRole("link", { name: "← 一覧へ" })).toHaveAttribute(
      "href",
      `/invoices?${from}`
    );

    expect(screen.getByRole("link", { name: "編集" })).toHaveAttribute(
      "href",
      `/invoices/12/edit?from=${encodeURIComponent(from)}`
    );

    const paymentLinks = screen.getAllByRole("link", { name: "入金登録" });
    expect(paymentLinks.length).toBeGreaterThan(0);
    paymentLinks.forEach((link) => {
      expect(link).toHaveAttribute(
        "href",
        `/payments/new?invoiceId=12&from=${encodeURIComponent(from)}`
      );
    });

    expect(screen.getByRole("link", { name: "督促へ" })).toHaveAttribute(
      "href",
      "/collections/12"
    );
  });

it("statusName ごとの表示を出す（未入金 / 一部入金 / 入金済み）", async () => {
  mockApiGetServer
    .mockResolvedValueOnce({
      id: 21,
      invoiceNumber: "INV-0021",
      statusName: "未入金",
      totalAmount: 10000,
      paidAmount: 0,
      remainingAmount: 10000,
      invoiceDate: "2026-05-01T00:00:00",
      dueDate: "2026-05-31T00:00:00",
      memberName: "株式会社A",
      remarks: null,
      reminders: [],
      allocations: [],
    } as any)
    .mockResolvedValueOnce({
      id: 22,
      invoiceNumber: "INV-0022",
      statusName: "一部入金",
      totalAmount: 20000,
      paidAmount: 5000,
      remainingAmount: 15000,
      invoiceDate: "2026-05-01T00:00:00",
      dueDate: "2026-05-31T00:00:00",
      memberName: "株式会社B",
      remarks: null,
      reminders: [],
      allocations: [],
    } as any)
    .mockResolvedValueOnce({
      id: 23,
      invoiceNumber: "INV-0023",
      statusName: "入金済み",
      totalAmount: 30000,
      paidAmount: 30000,
      remainingAmount: 0,
      invoiceDate: "2026-05-01T00:00:00",
      dueDate: "2026-05-31T00:00:00",
      memberName: "株式会社C",
      remarks: null,
      reminders: [],
      allocations: [],
    } as any);

  const unpaid = await InvoiceDetailPage({
    params: Promise.resolve({ id: "21" }),
    searchParams: Promise.resolve({}),
  });
  render(unpaid);
  expect(screen.getAllByText("未入金").length).toBeGreaterThan(0);

  const partial = await InvoiceDetailPage({
    params: Promise.resolve({ id: "22" }),
    searchParams: Promise.resolve({}),
  });
  render(partial);
  expect(screen.getAllByText("一部入金").length).toBeGreaterThan(0);

  const paid = await InvoiceDetailPage({
    params: Promise.resolve({ id: "23" }),
    searchParams: Promise.resolve({}),
  });
  render(paid);
  expect(screen.getAllByText("入金済み").length).toBeGreaterThan(0);
});

it("入金 method を表示変換する（銀行振込 / 現金 / カード / その他）", async () => {
  mockApiGetServer.mockResolvedValueOnce({
    id: 30,
    invoiceNumber: "INV-0030",
    statusName: "一部入金",
    totalAmount: 40000,
    paidAmount: 20000,
    remainingAmount: 20000,
    invoiceDate: "2026-06-01T00:00:00",
    dueDate: "2026-06-30T00:00:00",
    memberName: "株式会社D",
    remarks: null,
    reminders: [],
    allocations: [
      {
        paymentId: 301,
        paymentDate: "2026-06-10T00:00:00",
        method: "BANK_TRANSFER",
        payerName: "株式会社D",
        importBatchId: 1,
        allocatedAmount: 5000,
      },
      {
        paymentId: 302,
        paymentDate: "2026-06-11T00:00:00",
        method: "CASH",
        payerName: "株式会社D",
        importBatchId: 2,
        allocatedAmount: 5000,
      },
      {
        paymentId: 303,
        paymentDate: "2026-06-12T00:00:00",
        method: "CARD",
        payerName: "株式会社D",
        importBatchId: 3,
        allocatedAmount: 5000,
      },
      {
        paymentId: 304,
        paymentDate: "2026-06-13T00:00:00",
        method: "UNKNOWN_METHOD",
        payerName: "株式会社D",
        importBatchId: 4,
        allocatedAmount: 5000,
      },
    ],
  } as any);

  const element = await InvoiceDetailPage({
    params: Promise.resolve({ id: "30" }),
    searchParams: Promise.resolve({}),
  });

  render(element);

  expect(screen.getByText("2026-06-10 / 銀行振込")).toBeInTheDocument();
  expect(screen.getByText("2026-06-11 / 現金")).toBeInTheDocument();
  expect(screen.getByText("2026-06-12 / カード")).toBeInTheDocument();
  expect(screen.getByText("2026-06-13 / その他")).toBeInTheDocument();
});

it("payerName なし / importBatchId なしでも表示が崩れない", async () => {
  mockApiGetServer.mockResolvedValueOnce({
    id: 31,
    invoiceNumber: "INV-0031",
    statusName: "一部入金",
    totalAmount: 50000,
    paidAmount: 10000,
    remainingAmount: 40000,
    invoiceDate: "2026-06-01T00:00:00",
    dueDate: "2026-06-30T00:00:00",
    memberName: "株式会社E",
    remarks: null,
    reminders: [],
    allocations: [
      {
        paymentId: 401,
        paymentDate: "2026-06-20T00:00:00",
        method: "BANK_TRANSFER",
        payerName: null,
        importBatchId: null,
        allocatedAmount: 10000,
      },
    ],
  } as any);

  const element = await InvoiceDetailPage({
    params: Promise.resolve({ id: "31" }),
    searchParams: Promise.resolve({}),
  });

  render(element);

  expect(screen.getByText("2026-06-20 / 銀行振込")).toBeInTheDocument();
  expect(screen.getByText("名義：—")).toBeInTheDocument();
  expect(screen.getAllByText(/[¥￥]10,000/).length).toBeGreaterThan(0);
});

});