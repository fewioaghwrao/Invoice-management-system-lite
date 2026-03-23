import { render, screen } from "@testing-library/react";
import MembersPage from "@/app/members/page";
import { apiGetServer } from "@/lib/api.server";

jest.mock("@/lib/api.server", () => ({
  apiGetServer: jest.fn(),
}));

jest.mock("@/app/members/DeactivateMemberButton", () => ({
  DeactivateMemberButton: (props: {
    id: number;
    name: string;
    isActive: boolean;
    role: number;
  }) => (
    <button>
      DeactivateMemberButton:{props.id}:{props.name}:{String(props.isActive)}:
      {props.role}
    </button>
  ),
}));

const mockApiGetServer = apiGetServer as jest.MockedFunction<typeof apiGetServer>;

describe("MembersPage", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("会員一覧を表示し、管理者には退会ボタンを出さない", async () => {
    mockApiGetServer.mockResolvedValueOnce([
      {
        id: 1,
        name: "管理者 太郎",
        email: "admin@example.com",
        role: 1,
        isActive: true,
      },
      {
        id: 2,
        name: "会員 花子",
        email: "member@example.com",
        role: 2,
        isActive: true,
      },
      {
        id: 3,
        name: "退会済 一郎",
        email: "withdrawn@example.com",
        role: 9,
        isActive: false,
      },
    ] as any);

    const element = await MembersPage({
      searchParams: Promise.resolve({
        keyword: "example",
        role: "2",
        isActive: "true",
        page: "1",
      }),
    });

    render(element);

    expect(screen.getByText("会員一覧（管理者）")).toBeInTheDocument();
    expect(
      screen.getByText(
        "会員の基本情報・ロール・有効 / 無効状態を検索し、退会（無効化）操作を行います。"
      )
    ).toBeInTheDocument();

    expect(screen.getAllByText("管理者 太郎").length).toBeGreaterThan(0);
    expect(screen.getAllByText("admin@example.com").length).toBeGreaterThan(0);
    expect(screen.getAllByText("管理者").length).toBeGreaterThan(0);
    expect(screen.getAllByText("有効").length).toBeGreaterThan(0);

    expect(screen.getAllByText("会員 花子").length).toBeGreaterThan(0);
    expect(screen.getAllByText("member@example.com").length).toBeGreaterThan(0);
    expect(screen.getAllByText("一般会員").length).toBeGreaterThan(0);

    expect(screen.getAllByText("退会済 一郎").length).toBeGreaterThan(0);
    expect(screen.getAllByText("withdrawn@example.com").length).toBeGreaterThan(0);
    expect(screen.getAllByText("退会").length).toBeGreaterThan(0);
    expect(screen.getAllByText("無効").length).toBeGreaterThan(0);

    expect(mockApiGetServer).toHaveBeenCalledWith("/api/members", {
      Keyword: "example",
      RoleId: 2,
      IsActive: true,
      Page: 1,
      PageSize: 5,
    });

expect(
  screen.getAllByText("DeactivateMemberButton:2:会員 花子:true:2").length
).toBeGreaterThan(0);
expect(
  screen.getAllByText("DeactivateMemberButton:3:退会済 一郎:false:9").length
).toBeGreaterThan(0);

expect(
  screen.queryByText("DeactivateMemberButton:1:管理者 太郎:true:1")
).not.toBeInTheDocument();

    expect(screen.getAllByRole("link", { name: "詳細" }).length).toBeGreaterThan(0);

    expect(screen.getByRole("link", { name: "← 管理トップへ" })).toHaveAttribute(
      "href",
      "/dashboards/admin"
    );

    expect(screen.getByRole("link", { name: "リセット" })).toHaveAttribute(
      "href",
      "/members"
    );
  });

  it("会員が0件のとき空状態と0件表示を出す", async () => {
    mockApiGetServer.mockResolvedValueOnce([] as any);

    const element = await MembersPage({
      searchParams: Promise.resolve({
        page: "1",
      }),
    });

    render(element);

    expect(
      screen.getAllByText("該当する会員はいません。").length
    ).toBeGreaterThan(0);

    expect(screen.getByText("0件")).toBeInTheDocument();

    expect(screen.getByRole("link", { name: "前へ" })).toHaveAttribute("href", "#");
    expect(screen.getByRole("link", { name: "次へ" })).toHaveAttribute("href", "#");
  });

  it("次へ・前へリンクに検索条件を引き継ぐ", async () => {
    mockApiGetServer.mockResolvedValueOnce([
      {
        id: 11,
        name: "会員A",
        email: "a@example.com",
        role: 2,
        isActive: true,
      },
      {
        id: 12,
        name: "会員B",
        email: "b@example.com",
        role: 2,
        isActive: true,
      },
      {
        id: 13,
        name: "会員C",
        email: "c@example.com",
        role: 2,
        isActive: true,
      },
      {
        id: 14,
        name: "会員D",
        email: "d@example.com",
        role: 2,
        isActive: true,
      },
      {
        id: 15,
        name: "会員E",
        email: "e@example.com",
        role: 2,
        isActive: true,
      },
    ] as any);

    const element = await MembersPage({
      searchParams: Promise.resolve({
        keyword: "会員",
        role: "2",
        isActive: "false",
        page: "2",
      }),
    });

    render(element);

    expect(
      screen.getByText("6–10件を表示（1ページあたり 5件）")
    ).toBeInTheDocument();

    expect(screen.getByRole("link", { name: "前へ" })).toHaveAttribute(
      "href",
      "/members?keyword=%E4%BC%9A%E5%93%A1&role=2&isActive=false&page=1"
    );

    expect(screen.getByRole("link", { name: "次へ" })).toHaveAttribute(
      "href",
      "/members?keyword=%E4%BC%9A%E5%93%A1&role=2&isActive=false&page=3"
    );
  });

it("role と isActive の値に応じたラベルを表示する", async () => {
  mockApiGetServer.mockResolvedValueOnce([
    {
      id: 101,
      name: "管理者ユーザー",
      email: "admin-label@example.com",
      role: 1,
      isActive: true,
    },
    {
      id: 102,
      name: "一般会員ユーザー",
      email: "member-label@example.com",
      role: 2,
      isActive: true,
    },
    {
      id: 103,
      name: "退会ユーザー",
      email: "withdrawn-label@example.com",
      role: 9,
      isActive: false,
    },
    {
      id: 104,
      name: "不明ロールユーザー",
      email: "unknown-label@example.com",
      role: 999,
      isActive: false,
    },
  ] as any);

  const element = await MembersPage({
    searchParams: Promise.resolve({ page: "1" }),
  });

  render(element);

expect(screen.getAllByText("管理者").length).toBeGreaterThan(0);
expect(screen.getAllByText("一般会員").length).toBeGreaterThan(0);
expect(screen.getAllByText("退会").length).toBeGreaterThan(0);

expect(screen.getAllByText("有効").length).toBeGreaterThan(0);
expect(screen.getAllByText("無効").length).toBeGreaterThan(0);
});
it("page 未指定時は 1 ページ目として API を呼ぶ", async () => {
  mockApiGetServer.mockResolvedValueOnce([
    {
      id: 201,
      name: "会員A",
      email: "a@example.com",
      role: 2,
      isActive: true,
    },
  ] as any);

  const element = await MembersPage({
    searchParams: Promise.resolve({
      keyword: "A",
      role: "2",
      isActive: "true",
    }),
  });

  render(element);

  expect(mockApiGetServer).toHaveBeenCalledWith("/api/members", {
    Keyword: "A",
    RoleId: 2,
    IsActive: true,
    Page: 1,
    PageSize: 5,
  });

  expect(screen.getByText("1–1件を表示（1ページあたり 5件）")).toBeInTheDocument();
});

});

