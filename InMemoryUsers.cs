using System.Collections.Generic;

public static class InMemoryUsers
{
    public static List<User> Users = new List<User>
    {
        new User { Username = "admin", Password = "1234", Role = UserRole.Admin },
        new User { Username = "chief", Password = "1234", Role = UserRole.Chief },
        new User { Username = "manager", Password = "1234", Role = UserRole.Manager },
        new User { Username = "staff", Password = "1234", Role = UserRole.Staff },

    };
}
