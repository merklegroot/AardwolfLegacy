(function () {
    // https://stackoverflow.com/questions/105034/create-guid-uuid-in-javascript
    function uuidv4() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }

    var allDivs = document.getElementsByTagName("div");
    var matches = _.filter(allDivs, queryDiv => {
        var divClass = queryDiv.getAttribute("class");
        if (divClass === null || divClass !== "basicPanel") { return false; }
        return _.some(queryDiv.getElementsByTagName("header"), queryHeader => {
            return queryHeader.innerText !== null && queryHeader.innerText.trim().toUpperCase() === "MY OPEN ORDERS";
        });
    });

    var match = matches !== null && matches.length === 1 ? matches[0] : null;
    var tbody = null;
    if (match !== null) {
        var tbodies = match.getElementsByTagName("tbody");
        tbody = tbodies !== null && tbodies.length === 1 ? tbodies[0] : null;
    }

    var openOrders = [];
    if (tbody !== null) {
        var rows = tbody.getElementsByTagName("tr");
        for (var rowIndex = 0; rows !== null && rowIndex < rows.length; rowIndex++) {
            var row = rows[rowIndex];
            var cells = row.getElementsByTagName("td");
            if (cells === null || cells.length !== 6) { continue; }
            // Type	| Price	      | Amount (APPC) | Total(ETH) | Date                | Action
            // Buy  | 0.00038169  | 1309.96489298 | 0.5000005  | 2018-07-12 17:21:13 | Cancel
            
            row.id = uuidv4();

            openOrders.push({
                rowId: row.id,
                operation: cells[0].innerText,
                price: cells[1].innerText,
                symbolQuantity: cells[2].innerText,
                ethQuantity: cells[3].innerText,
                date: cells[4].innerText
            });
        }
    }

    var debugElement = document.getElementById('openOrdersDebug');
    if (debugElement !== null) {
        debugElement.innerText = JSON.stringify(openOrders);
    } else {
        debugElement = document.createElement('div');
        debugElement.id = 'openOrdersDebug';
        debugElement.innerText = JSON.stringify(openOrders);
        document.body.appendChild(debugElement);
    }
})();