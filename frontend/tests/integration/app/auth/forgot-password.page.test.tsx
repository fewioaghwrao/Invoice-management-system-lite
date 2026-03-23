import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import ForgotPasswordPage from "../../../../src/app/auth/forgot-password/page";

describe("ForgotPasswordPage", () => {
  const fetchMock = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    global.fetch = fetchMock as unknown as typeof fetch;
  });

  it("見出し・説明文・入力欄・送信ボタンを表示する", () => {
    render(<ForgotPasswordPage />);

    expect(screen.getByText("パスワード再設定")).toBeInTheDocument();
    expect(
      screen.getByText("登録したメールアドレス宛に再設定リンクを送信します。")
    ).toBeInTheDocument();

    expect(screen.getByLabelText("メールアドレス")).toBeInTheDocument();
    expect(
      screen.getByPlaceholderText("example@example.com")
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "再設定メールを送信" })
    ).toBeInTheDocument();
  });

  it("メールアドレスを入力して送信成功時に完了メッセージを表示する", async () => {
    const user = userEvent.setup();

    fetchMock.mockResolvedValueOnce({
      ok: true,
    });

    render(<ForgotPasswordPage />);

    await user.type(
      screen.getByLabelText("メールアドレス"),
      "test@example.com"
    );
    await user.click(screen.getByRole("button", { name: "再設定メールを送信" }));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(1);
    });

    expect(fetchMock).toHaveBeenCalledWith(
      "http://localhost:5101/auth/forgot-password",
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email: "test@example.com" }),
      }
    );

  expect(
  await screen.findByText(/再設定リンクをメールに送信しました。/)
).toBeInTheDocument();
expect(screen.getByText(/メールをご確認ください。/)).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "再設定メールを送信" })
    ).not.toBeInTheDocument();
  });

  it("API が ok=false のときエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    fetchMock.mockResolvedValueOnce({
      ok: false,
    });

    render(<ForgotPasswordPage />);

    await user.type(
      screen.getByLabelText("メールアドレス"),
      "invalid@example.com"
    );
    await user.click(screen.getByRole("button", { name: "再設定メールを送信" }));

    expect(
      await screen.findByText(
        "メール送信に失敗しました。メールアドレスをご確認ください。"
      )
    ).toBeInTheDocument();

    expect(
      screen.queryByText("再設定リンクをメールに送信しました。")
    ).not.toBeInTheDocument();
  });

  it("通信エラー時にエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    fetchMock.mockRejectedValueOnce(new Error("network error"));

    render(<ForgotPasswordPage />);

    await user.type(
      screen.getByLabelText("メールアドレス"),
      "test@example.com"
    );
    await user.click(screen.getByRole("button", { name: "再設定メールを送信" }));

    expect(
      await screen.findByText("通信エラーが発生しました。")
    ).toBeInTheDocument();
  });

  it("再送信時に前回エラーをクリアする", async () => {
    const user = userEvent.setup();

    fetchMock
      .mockResolvedValueOnce({
        ok: false,
      })
      .mockResolvedValueOnce({
        ok: true,
      });

    render(<ForgotPasswordPage />);

    await user.type(
      screen.getByLabelText("メールアドレス"),
      "test@example.com"
    );

    await user.click(screen.getByRole("button", { name: "再設定メールを送信" }));

    expect(
      await screen.findByText(
        "メール送信に失敗しました。メールアドレスをご確認ください。"
      )
    ).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "再設定メールを送信" }));

    await waitFor(() => {
      expect(
        screen.queryByText(
          "メール送信に失敗しました。メールアドレスをご確認ください。"
        )
      ).not.toBeInTheDocument();
    });

expect(
  await screen.findByText(
    (_, element) =>
      element?.tagName.toLowerCase() === "p" &&
      (element.textContent?.includes("再設定リンクをメールに送信しました。") ?? false)
  )
).toBeInTheDocument();
  });
});