import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import CollectionsClient from "../../../../src/app/collections/[invoiceId]/CollectionsClient";
import { apiGetClient, apiPostClient } from "@/lib/api.client";

jest.mock("next/link", () => {
  return function MockLink({
    href,
    children,
    className,
  }: {
    href: string;
    children: React.ReactNode;
    className?: string;
  }) {
    return (
      <a href={href} className={className}>
        {children}
      </a>
    );
  };
});

jest.mock("@/lib/api.client", () => ({
  apiGetClient: jest.fn(),
  apiPostClient: jest.fn(),
}));

const mockApiGetClient = apiGetClient as jest.MockedFunction<typeof apiGetClient>;
const mockApiPostClient = apiPostClient as jest.MockedFunction<typeof apiPostClient>;

const submitButtonName = "督促処理を受け付ける";

function createSnapshot(overrides?: Partial<any>) {
  return {
    invoiceId: "1",
    invoiceNumber: "INV-001",
    memberName: "山田 太郎",
    memberEmail: "yamada@example.com",
    invoiceDate: "2026-03-01T00:00:00",
    dueDate: "2026-03-10T00:00:00",
    total: 100000,
    paidTotal: 30000,
    ...overrides,
  };
}

function createLog(overrides?: Partial<any>) {
  return {
    id: "log-1",
    at: "2026-03-15",
    channel: "EMAIL",
    title: "初回督促（標準）",
    memo: "送付済み",
    tone: "NORMAL",
    nextActionDate: "2026-03-20",
    ...overrides,
  };
}

describe("CollectionsClient", () => {
  const originalClipboard = navigator.clipboard;

  beforeEach(() => {
    jest.clearAllMocks();

    Object.defineProperty(window, "alert", {
      configurable: true,
      value: jest.fn(),
    });

    Object.defineProperty(window, "confirm", {
      configurable: true,
      value: jest.fn(),
    });

    Object.defineProperty(navigator, "clipboard", {
      configurable: true,
      value: {
        writeText: jest.fn().mockResolvedValue(undefined),
      },
    });
  });

  afterEach(() => {
    Object.defineProperty(navigator, "clipboard", {
      configurable: true,
      value: originalClipboard,
    });
  });

  it("初期ロード成功時に見出し・スナップショット・履歴を表示する", async () => {
    mockApiGetClient
      .mockResolvedValueOnce(createSnapshot())
      .mockResolvedValueOnce([createLog()]);

    render(<CollectionsClient invoiceId="1" />);

    expect(screen.getByText("読み込み中…")).toBeInTheDocument();

    expect(await screen.findByText("督促（INV-001）")).toBeInTheDocument();
    expect(
      screen.getByText(
        "テンプレ選択 → 文面プレビュー → 履歴管理 → 次アクションまで一画面で管理します。"
      )
    ).toBeInTheDocument();

    expect(screen.getByText("山田 太郎")).toBeInTheDocument();
    expect(screen.getByText("yamada@example.com")).toBeInTheDocument();
    expect(screen.getByText("2026-03-10")).toBeInTheDocument();
    expect(screen.getByText("請求日：2026-03-01")).toBeInTheDocument();

    expect(screen.getByText("￥100,000")).toBeInTheDocument();
    expect(screen.getByText("入金済：￥30,000")).toBeInTheDocument();
    expect(screen.getByText("￥70,000")).toBeInTheDocument();

    expect(screen.getByText("初回督促（標準）")).toBeInTheDocument();
    expect(screen.getByText("送付済み")).toBeInTheDocument();

    expect(mockApiGetClient).toHaveBeenNthCalledWith(
      1,
      "/api/collections/1/snapshot"
    );
    expect(mockApiGetClient).toHaveBeenNthCalledWith(
      2,
      "/api/collections/1/logs"
    );
  });

  it("from があるとパンくずと戻り先リンクに反映する", async () => {
    mockApiGetClient
      .mockResolvedValueOnce(createSnapshot())
      .mockResolvedValueOnce([createLog()]);

    render(<CollectionsClient invoiceId="1" from="year=2026&page=2" />);

    expect(await screen.findByText("督促（INV-001）")).toBeInTheDocument();

    expect(screen.getByRole("link", { name: "請求書一覧" })).toHaveAttribute(
      "href",
      "/invoices?year=2026&page=2"
    );

    expect(
      screen.queryByRole("link", { name: "請求書一覧へ戻る" })
    ).not.toBeInTheDocument();

    expect(screen.getByRole("link", { name: "INV-001" })).toHaveAttribute(
      "href",
      "/invoices/1?from=year%3D2026%26page%3D2"
    );

    expect(screen.getByRole("link", { name: "← 請求書詳細へ" })).toHaveAttribute(
      "href",
      "/invoices/1?from=year%3D2026%26page%3D2"
    );
  });

  it("履歴が0件のとき『履歴はまだありません。』を表示し、次回アクション日に支払期限を初期表示する", async () => {
    mockApiGetClient
      .mockResolvedValueOnce(createSnapshot())
      .mockResolvedValueOnce([]);

    render(<CollectionsClient invoiceId="1" />);

    expect(await screen.findByText("督促（INV-001）")).toBeInTheDocument();

    expect(screen.getByText("履歴はまだありません。")).toBeInTheDocument();
    expect(screen.getByDisplayValue("2026-03-10")).toBeInTheDocument();
  });

  it("最新ログに nextActionDate があればそれを次回アクション日の初期値にする", async () => {
    mockApiGetClient
      .mockResolvedValueOnce(createSnapshot({ dueDate: "2026-03-10T00:00:00" }))
      .mockResolvedValueOnce([
        createLog({ nextActionDate: "2026-03-25T00:00:00" }),
      ]);

    render(<CollectionsClient invoiceId="1" />);

    expect(await screen.findByText("督促（INV-001）")).toBeInTheDocument();
    expect(screen.getByDisplayValue("2026-03-25")).toBeInTheDocument();
  });

  it("初期ロード失敗時にエラー表示する", async () => {
    mockApiGetClient.mockRejectedValueOnce(new Error("load failed"));

    render(<CollectionsClient invoiceId="1" />);

    expect(await screen.findByText("エラー")).toBeInTheDocument();
    expect(screen.getByText("load failed")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "請求書一覧へ戻る" })).toHaveAttribute(
      "href",
      "/invoices"
    );
  });

  it("clipboard がない環境では本文コピーボタンを表示しない", async () => {
    Object.defineProperty(navigator, "clipboard", {
      configurable: true,
      value: undefined,
    });

    mockApiGetClient
      .mockResolvedValueOnce(createSnapshot())
      .mockResolvedValueOnce([]);

    render(<CollectionsClient invoiceId="1" />);

    expect(await screen.findByText("督促（INV-001）")).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "本文コピー" })
    ).not.toBeInTheDocument();
  });

  it("本文コピーを押すと本文をコピーして完了メッセージを出す", async () => {
    const user = userEvent.setup();
    const writeText = jest.fn().mockResolvedValue(undefined);

    Object.defineProperty(navigator, "clipboard", {
      configurable: true,
      value: { writeText },
    });

    mockApiGetClient
      .mockResolvedValueOnce(createSnapshot())
      .mockResolvedValueOnce([]);

    render(<CollectionsClient invoiceId="1" />);

    expect(await screen.findByText("督促（INV-001）")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "本文コピー" }));

    expect(writeText).toHaveBeenCalledTimes(1);
    expect(writeText.mock.calls[0][0]).toContain("山田 太郎 様");
    expect(writeText.mock.calls[0][0]).toContain("【請求書番号】INV-001");
    expect(window.alert).toHaveBeenCalledWith("本文をコピーしました。");
  });

  it("トーンを変えると件名プレビューが切り替わる", async () => {
    const user = userEvent.setup();

    mockApiGetClient
      .mockResolvedValueOnce(createSnapshot())
      .mockResolvedValueOnce([]);

    render(<CollectionsClient invoiceId="1" />);

    expect(await screen.findByText("督促（INV-001）")).toBeInTheDocument();

    expect(
      screen.getByText("【重要】お支払いのお願い（INV-001）")
    ).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "ソフト" }));
    expect(
      screen.getByText("【ご確認】お支払い状況のご確認のお願い（INV-001）")
    ).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "強め" }));
    expect(
      screen.getByText("【至急】お支払いのお願い（INV-001）")
    ).toBeInTheDocument();
  });

  it("未回収残額が0円のときは記録せず alert を出す", async () => {
    const user = userEvent.setup();

    mockApiGetClient
      .mockResolvedValueOnce(createSnapshot({ paidTotal: 100000 }))
      .mockResolvedValueOnce([]);

    render(<CollectionsClient invoiceId="1" />);

    expect(await screen.findByText("督促（INV-001）")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: submitButtonName }));

    expect(window.alert).toHaveBeenCalledWith(
      "未回収残額が 0 円のため、督促の記録は不要です。"
    );
    expect(mockApiPostClient).not.toHaveBeenCalled();
  });

  it("confirm でキャンセルしたときは記録しない", async () => {
    const user = userEvent.setup();
    (window.confirm as jest.Mock).mockReturnValue(false);

    mockApiGetClient
      .mockResolvedValueOnce(createSnapshot())
      .mockResolvedValueOnce([]);

    render(<CollectionsClient invoiceId="1" />);

    expect(await screen.findByText("督促（INV-001）")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: submitButtonName }));

    expect(window.confirm).toHaveBeenCalledTimes(1);
expect(String((window.confirm as jest.Mock).mock.calls[0][0])).toContain(
  "督促処理を受け付けます。よろしいですか？"
);
    expect(String((window.confirm as jest.Mock).mock.calls[0][0])).toContain(
      "請求書：INV-001"
    );
    expect(String((window.confirm as jest.Mock).mock.calls[0][0])).toContain(
      "チャネル：メール"
    );
    expect(String((window.confirm as jest.Mock).mock.calls[0][0])).toContain(
      "トーン：標準"
    );

    expect(mockApiPostClient).not.toHaveBeenCalled();
  });

  it("記録成功時は POST して履歴を再取得し、成功メッセージを出す", async () => {
    const user = userEvent.setup();
    (window.confirm as jest.Mock).mockReturnValue(true);

    mockApiGetClient
      .mockResolvedValueOnce(createSnapshot())
      .mockResolvedValueOnce([])
      .mockResolvedValueOnce([
        createLog({
          id: "log-2",
          title: "初回督促（標準）",
          memo: "テンプレ送付。",
          nextActionDate: "2026-03-10",
        }),
      ]);

    mockApiPostClient.mockResolvedValueOnce({});

    render(<CollectionsClient invoiceId="1" />);

    expect(await screen.findByText("督促（INV-001）")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: submitButtonName }));

    await waitFor(() => {
      expect(mockApiPostClient).toHaveBeenCalledTimes(1);
    });

    expect(mockApiPostClient).toHaveBeenCalledWith(
      "/api/collections/1/logs",
      expect.objectContaining({
        channel: "EMAIL",
        tone: "NORMAL",
        title: "初回督促（標準）",
        memo: "テンプレ送付。",
        nextActionDate: "2026-03-10",
        subject: "【重要】お支払いのお願い（INV-001）",
      })
    );

    expect(window.alert).toHaveBeenCalledWith(
      "督促処理を受け付けました。メール送信はバックグラウンドで処理されます。"
    );
    expect(await screen.findByText("初回督促（標準）")).toBeInTheDocument();
    expect(screen.getByText("テンプレ送付。")).toBeInTheDocument();
  });

  it("チャネル・トーン・次回アクション日を変更して記録できる", async () => {
    const user = userEvent.setup();
    (window.confirm as jest.Mock).mockReturnValue(true);

    mockApiGetClient
      .mockResolvedValueOnce(createSnapshot())
      .mockResolvedValueOnce([createLog()])
      .mockResolvedValueOnce([
        createLog(),
        createLog({
          id: "log-2",
          channel: "PHONE",
          tone: "STRONG",
          title: "督促（2回目 / 強め）",
          memo: "テンプレ送付。",
          nextActionDate: "2026-03-30",
        }),
      ]);

    mockApiPostClient.mockResolvedValueOnce({});

    render(<CollectionsClient invoiceId="1" />);

    expect(await screen.findByText("督促（INV-001）")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "電話" }));
    await user.click(screen.getByRole("button", { name: "強め" }));

    const nextActionInput = screen.getByDisplayValue("2026-03-20");
    await user.clear(nextActionInput);
    await user.type(nextActionInput, "2026-03-30");

    await user.click(screen.getByRole("button", { name: submitButtonName }));

    expect(String((window.confirm as jest.Mock).mock.calls[0][0])).toContain(
      "チャネル：電話"
    );
    expect(String((window.confirm as jest.Mock).mock.calls[0][0])).toContain(
      "トーン：強め"
    );
    expect(String((window.confirm as jest.Mock).mock.calls[0][0])).toContain(
      "次回アクション日：2026-03-30"
    );

    await waitFor(() => {
      expect(mockApiPostClient).toHaveBeenCalledWith(
        "/api/collections/1/logs",
        expect.objectContaining({
          channel: "PHONE",
          tone: "STRONG",
          title: "督促（2回目 / 強め）",
          nextActionDate: "2026-03-30",
          subject: "【至急】お支払いのお願い（INV-001）",
        })
      );
    });

    expect(await screen.findByText("督促（2回目 / 強め）")).toBeInTheDocument();
  });
});