import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import LoginPage from "../../../../src/app/auth/login/page";

const pushMock = jest.fn();
const replaceMock = jest.fn();

jest.mock("next/navigation", () => ({
  useRouter: () => ({
    push: pushMock,
    replace: replaceMock,
  }),
}));

describe("LoginPage", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    global.fetch = jest.fn() as jest.Mock;
  });

    afterEach(() => {
    jest.useRealTimers();
  });

  it("初期表示で主要要素を表示する", () => {
    render(<LoginPage />);

    expect(screen.getByRole("heading", { name: "ログイン" })).toBeInTheDocument();
    expect(screen.getByLabelText("メールアドレス")).toBeInTheDocument();
    expect(screen.getByLabelText("パスワード")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "ログイン" })).toBeInTheDocument();

    expect(screen.getByRole("button", { name: "新規会員登録" })).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "パスワードをお忘れの方はこちら" })
    ).toBeInTheDocument();

    expect(
      screen.getByText("デモログイン（パスワードは画面に表示しません）")
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "管理者デモでログイン" })
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "一般会員デモでログイン" })
    ).toBeInTheDocument();
  });

  it("パスワードの表示と非表示を切り替えできる", async () => {
    const user = userEvent.setup();

    render(<LoginPage />);

    const passwordInput = screen.getByLabelText("パスワード");
    expect(passwordInput).toHaveAttribute("type", "password");

    await user.click(screen.getByRole("button", { name: "表示" }));
    expect(passwordInput).toHaveAttribute("type", "text");

    await user.click(screen.getByRole("button", { name: "非表示" }));
    expect(passwordInput).toHaveAttribute("type", "password");
  });

  it("管理者ログイン成功時は管理者ダッシュボードへ遷移する", async () => {
    const user = userEvent.setup();

    (global.fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        token: "dummy-token",
        role: "Admin",
      }),
    });

    render(<LoginPage />);

    await user.type(screen.getByLabelText("メールアドレス"), "admin@example.com");
    await user.type(screen.getByLabelText("パスワード"), "password123");
    await user.click(screen.getByRole("button", { name: "ログイン" }));

    await waitFor(() => {
      expect(global.fetch).toHaveBeenCalledWith(
        "http://localhost:5101/auth/login",
        expect.objectContaining({
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            email: "admin@example.com",
            password: "password123",
          }),
        })
      );
    });

    await waitFor(() => {
      expect(pushMock).toHaveBeenCalledWith("/dashboards/admin");
    });
  });

  it("一般会員ログイン成功時は請求一覧へ遷移する", async () => {
    const user = userEvent.setup();

    (global.fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        token: "dummy-token",
        role: "Member",
      }),
    });

    render(<LoginPage />);

    await user.type(screen.getByLabelText("メールアドレス"), "member@example.com");
    await user.type(screen.getByLabelText("パスワード"), "password123");
    await user.click(screen.getByRole("button", { name: "ログイン" }));

    await waitFor(() => {
     expect(pushMock).toHaveBeenCalledWith("/dashboards/member");
    });
  });

  it("401 のとき認証エラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    (global.fetch as jest.Mock).mockResolvedValueOnce({
      ok: false,
      status: 401,
      json: async () => ({}),
    });

    render(<LoginPage />);

    await user.type(screen.getByLabelText("メールアドレス"), "test@example.com");
    await user.type(screen.getByLabelText("パスワード"), "wrong-password");
    await user.click(screen.getByRole("button", { name: "ログイン" }));

    expect(
      await screen.findByText("メールアドレスまたはパスワードが正しくありません。")
    ).toBeInTheDocument();
  });

  it("403 かつ message なしのとき専用メッセージを表示する", async () => {
    const user = userEvent.setup();

    (global.fetch as jest.Mock).mockResolvedValueOnce({
      ok: false,
      status: 403,
      json: async () => ({}),
    });

    render(<LoginPage />);

    await user.type(screen.getByLabelText("メールアドレス"), "test@example.com");
    await user.type(screen.getByLabelText("パスワード"), "password123");
    await user.click(screen.getByRole("button", { name: "ログイン" }));

    expect(
      await screen.findByText("このアカウントは利用できません。")
    ).toBeInTheDocument();
  });

  it("400 かつ message なしのとき専用メッセージを表示する", async () => {
    const user = userEvent.setup();

    (global.fetch as jest.Mock).mockResolvedValueOnce({
      ok: false,
      status: 400,
      json: async () => ({}),
    });

    render(<LoginPage />);

    await user.type(screen.getByLabelText("メールアドレス"), "test@example.com");
    await user.type(screen.getByLabelText("パスワード"), "password123");
    await user.click(screen.getByRole("button", { name: "ログイン" }));

    expect(
      await screen.findByText("不正なリクエストです。")
    ).toBeInTheDocument();
  });

  it("API が message を返すときはその内容を表示する", async () => {
    const user = userEvent.setup();

    (global.fetch as jest.Mock).mockResolvedValueOnce({
      ok: false,
      status: 400,
      json: async () => ({
        message: "メール認証が完了していません。",
      }),
    });

    render(<LoginPage />);

    await user.type(screen.getByLabelText("メールアドレス"), "test@example.com");
    await user.type(screen.getByLabelText("パスワード"), "password123");
    await user.click(screen.getByRole("button", { name: "ログイン" }));

    expect(
      await screen.findByText("メール認証が完了していません。")
    ).toBeInTheDocument();
  });

  it("500 のとき既定の失敗メッセージを表示する", async () => {
    const user = userEvent.setup();

    (global.fetch as jest.Mock).mockResolvedValueOnce({
      ok: false,
      status: 500,
      json: async () => ({}),
    });

    render(<LoginPage />);

    await user.type(screen.getByLabelText("メールアドレス"), "test@example.com");
    await user.type(screen.getByLabelText("パスワード"), "password123");
    await user.click(screen.getByRole("button", { name: "ログイン" }));

    expect(
      await screen.findByText("ログインに失敗しました。時間をおいて再度お試しください。")
    ).toBeInTheDocument();
  });

  it("通信エラー時はエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    (global.fetch as jest.Mock).mockRejectedValueOnce(new Error("network error"));

    render(<LoginPage />);

    await user.type(screen.getByLabelText("メールアドレス"), "test@example.com");
    await user.type(screen.getByLabelText("パスワード"), "password123");
    await user.click(screen.getByRole("button", { name: "ログイン" }));

expect(
  await screen.findByText(
    "通信エラーが発生しました。ネットワーク環境を確認してから再度お試しください。"
  )
).toBeInTheDocument();
  });

  it("送信中はログインボタンが無効化される", async () => {
    const user = userEvent.setup();

    let resolveFetch!: (value: any) => void;
    (global.fetch as jest.Mock).mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          resolveFetch = resolve;
        })
    );

    render(<LoginPage />);

    await user.type(screen.getByLabelText("メールアドレス"), "admin@example.com");
    await user.type(screen.getByLabelText("パスワード"), "password123");
    await user.click(screen.getByRole("button", { name: "ログイン" }));

    expect(screen.getByRole("button", { name: "ログイン中..." })).toBeDisabled();

    resolveFetch({
      ok: true,
      json: async () => ({
        token: "dummy-token",
        role: "Admin",
      }),
    });

    await waitFor(() => {
      expect(pushMock).toHaveBeenCalledWith("/dashboards/admin");
    });
  });

  it("管理者デモでログインできる", async () => {
    jest.useFakeTimers();
    const user = userEvent.setup({ advanceTimers: jest.advanceTimersByTime });

    (global.fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        token: "demo-token",
        role: "Admin",
      }),
    });

    render(<LoginPage />);

    await user.click(screen.getByRole("button", { name: "管理者デモでログイン" }));

    jest.runAllTimers();

    await waitFor(() => {
      expect(global.fetch).toHaveBeenCalledWith(
        "http://localhost:5101/auth/login",
        expect.objectContaining({
          method: "POST",
          body: JSON.stringify({
            email: "admin@example.com",
            password: "Admin1234!",
          }),
        })
      );
    });

    await waitFor(() => {
      expect(pushMock).toHaveBeenCalledWith("/dashboards/admin");
    });

    jest.useRealTimers();
  });

  it("一般会員デモでログインできる", async () => {
    jest.useFakeTimers();
    const user = userEvent.setup({ advanceTimers: jest.advanceTimersByTime });

    (global.fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        token: "demo-token",
        role: "Member",
      }),
    });

    render(<LoginPage />);

    await user.click(screen.getByRole("button", { name: "一般会員デモでログイン" }));

    jest.runAllTimers();

    await waitFor(() => {
      expect(global.fetch).toHaveBeenCalledWith(
        "http://localhost:5101/auth/login",
        expect.objectContaining({
          method: "POST",
          body: JSON.stringify({
            email: "member@example.com",
            password: "Member1234!",
          }),
        })
      );
    });

    await waitFor(() => {
   expect(pushMock).toHaveBeenCalledWith("/dashboards/member");
    });

    jest.useRealTimers();
  });

  it("新規会員登録ボタン押下で登録画面へ遷移する", async () => {
    const user = userEvent.setup();

    render(<LoginPage />);

    await user.click(screen.getByRole("button", { name: "新規会員登録" }));

    expect(pushMock).toHaveBeenCalledWith("/auth/register");
  });

  it("パスワードをお忘れの方はこちら押下で再設定画面へ遷移する", async () => {
    const user = userEvent.setup();

    render(<LoginPage />);

    await user.click(
      screen.getByRole("button", { name: "パスワードをお忘れの方はこちら" })
    );

    expect(pushMock).toHaveBeenCalledWith("/auth/forgot-password");
  });

it("送信中にデモログインを押しても submit しない", async () => {
  const user = userEvent.setup();

  let resolveFetch!: (value: any) => void;
  (global.fetch as jest.Mock).mockImplementationOnce(
    () =>
      new Promise((resolve) => {
        resolveFetch = resolve;
      })
  );

  render(<LoginPage />);

  await user.type(screen.getByLabelText("メールアドレス"), "admin@example.com");
  await user.type(screen.getByLabelText("パスワード"), "password123");
  await user.click(screen.getByRole("button", { name: "ログイン" }));

  expect(screen.getByRole("button", { name: "ログイン中..." })).toBeDisabled();

  await user.click(screen.getByRole("button", { name: "管理者デモでログイン" }));

  expect(global.fetch).toHaveBeenCalledTimes(1);

  resolveFetch({
    ok: true,
    json: async () => ({
      token: "dummy-token",
      role: "Admin",
    }),
  });

  await waitFor(() => {
    expect(pushMock).toHaveBeenCalledWith("/dashboards/admin");
  });
});

it("デモログインで setTimeout 後に requestSubmit が呼ばれる", async () => {
  jest.useFakeTimers();
  const user = userEvent.setup({ advanceTimers: jest.advanceTimersByTime });

  const requestSubmitSpy = jest.spyOn(HTMLFormElement.prototype, "requestSubmit");

  (global.fetch as jest.Mock).mockResolvedValueOnce({
    ok: true,
    json: async () => ({
      token: "demo-token",
      role: "Admin",
      email: "admin@example.com",
    }),
  });

  render(<LoginPage />);

  await user.click(screen.getByRole("button", { name: "管理者デモでログイン" }));

  expect(requestSubmitSpy).toHaveBeenCalled();

  await waitFor(() => {
    expect(pushMock).toHaveBeenCalledWith("/dashboards/admin");
  });

  requestSubmitSpy.mockRestore();
});

});