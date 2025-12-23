// src/types/member.ts

// C# の MemberRole に合わせた enum
export type MemberRoleCode = 1 | 2 | 9;

export type MemberListItemDto = {
  id: number;
  name: string;
  email: string;
  role: MemberRoleCode;   // ★ 数値で受ける
  isActive: boolean;
};
