namespace Mabar.PhaserExporter.Editor
{
    public static class HtmlTemplate
    {
        public static string Generate(PhaserProjectSettings s)
        {
            return
$@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>{s.gameName}</title>
    <style>
        *, *::before, *::after {{ box-sizing: border-box; margin: 0; padding: 0; }}
        body {{
            background: #000;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            overflow: hidden;
        }}
        canvas {{ display: block; }}
    </style>
</head>
<body>
    <script src=""https://cdn.jsdelivr.net/npm/phaser@{s.phaserVersion}/dist/phaser.min.js""></script>
    <script src=""game.js""></script>
</body>
</html>";
        }
    }
}
