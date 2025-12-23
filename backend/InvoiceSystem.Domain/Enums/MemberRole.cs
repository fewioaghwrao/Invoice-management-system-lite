using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceSystem.Domain.Enums
{
    public enum MemberRole
    {
        Admin = 1,      // 管理者
        Customer = 2,   // 一般会員
        Disabled = 9    // 退会（無効）
    }
}
