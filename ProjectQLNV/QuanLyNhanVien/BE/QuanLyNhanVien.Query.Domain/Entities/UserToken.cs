using System;
using System.Collections.Generic;

namespace QuanLyNhanVien.Query.Domain.Entities;

public partial class UserToken
{
    public int TokenId { get; set; }

    public int UserId { get; set; }

    public string AccessToken { get; set; } = null!;

    public string RefreshToken { get; set; } = null!;

    public DateTime AccessTokenExpiryDate { get; set; }

    public DateTime RefreshTokenExpiryDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
