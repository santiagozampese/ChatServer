public class Message
{
    public string? UserName {get; set;}
    public string? Color {get; set;}
    public string? Msg {get; set;}

    public string? toRoom {get; set;}

    public Message(string message, string? user, string? color)
    {
        Msg = message;
        UserName = user;
        Color = color;
    }

    public string GetFormattedMessage()
    {
        string message = Msg ?? "";
        if (!string.IsNullOrEmpty(UserName))
        {
            string? name="";
            if (User.Name==UserName) name = "You";
            else name = UserName;
            message = $"{Color}{name}{ConsoleHelper.ResetColor}: {Msg}";         
        }
        
        else
        {
            message = $"{Color}{Msg}{ConsoleHelper.ResetColor}";
        }
        return message;
    }
}