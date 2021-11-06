(function () {
    var desiredSymbol = "[SYMBOL]";
    var tbodies = document.getElementsByTagName("tbody");
    for (var i = 0; tbodies !== null && i < tbodies.length; i++) {
        var tbody = tbodies[i];
        var rows = tbody.getElementsByTagName("tr");
        for (var rowIndex = 0; rows !== null && rowIndex < rows.length; rowIndex++) {
            var row = rows[rowIndex];
            var cells = row.getElementsByTagName("td");
            if (cells === null || cells.length !== 6) { continue; }
            var rowSymbol = cells[0].innerText.trim();
            if (rowSymbol !== desiredSymbol) {
                continue;
            }

            var buttons = cells[4].getElementsByTagName("button");
            for (var buttonIndex = 0; buttons !== null && buttonIndex < buttons.length; buttonIndex++) {
                var button = buttons[buttonIndex];
                var buttonText = button.innerText.trim();
                if (buttonText === "Withdraw") {
                    button.click();
                    return;
                }
            }

            return;
        }
    }
})();
