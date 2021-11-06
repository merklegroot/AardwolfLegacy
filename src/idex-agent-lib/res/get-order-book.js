(function () {
    // https://stackoverflow.com/questions/105034/create-guid-uuid-in-javascript
    var uuidv4 = function () {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }

    var getPanel = function (key) {
        var divs = document.getElementsByTagName("div");
        var matchingPanels = _.filter(divs, queryDiv => {
            if (queryDiv.getAttribute("class") !== "basicPanel") { return false; }

            var headers = queryDiv.getElementsByTagName("header");
            if (!headers) { return false; }

            return _.some(headers, queryHeader => {
                var headerDivs = queryHeader.getElementsByTagName("div");
                if (!headerDivs) { return false; }
                return _.some(headerDivs, queryHeaderDiv => {
                    return queryHeaderDiv.innerText !== undefined && queryHeaderDiv.innerText !== null
                        && queryHeaderDiv.innerText.trim().toUpperCase() === key.toUpperCase();
                });
            });
        });

        return matchingPanels !== null && matchingPanels.length === 1 ? matchingPanels[0] : null;
    };

    var getPanelOrders = function (matchingPanel) {
        var table = null;
        if (matchingPanel !== null) {

            var tables = matchingPanel.getElementsByTagName("table");
            table = tables !== null && tables.length === 1 ? tables[0] : null;
        }

        var tbody = null;
        if (table !== null) {
            var tbodies = table.getElementsByTagName("tbody");
            tbody = tbodies !== null && tbodies.length === 1 ? tbodies[0] : null;
        }

        var rows = null;
        if (tbody !== null) {
            rows = tbody.getElementsByTagName("tr");
        }

        var orders = [];
        for (var rowIndex = 0; rows !== null && rowIndex < rows.length; rowIndex++) {
            var row = rows[rowIndex];
            var cells = row.getElementsByTagName("td");
            if (cells === null || cells.length !== 4) { continue; }
            row.id = uuidv4();

            orders.push({
                rowId: row.id,
                price: cells[0].innerText,
                symbolQuantity: cells[1].innerText,
                ethQuantity: cells[2].innerText,
                sum: cells[3].innerText
            });
        }

        return orders;
    }

    var orderBook = {
        asks: getPanelOrders(getPanel("ASKS")),
        bids: getPanelOrders(getPanel("BIDS"))
    };

    var debugElement = document.getElementById('orderBookDebug');
    if (debugElement !== null) {
        debugElement.innerText = JSON.stringify(orderBook);
    } else {
        debugElement = document.createElement('div');
        debugElement.id = 'orderBookDebug';
        debugElement.innerText = JSON.stringify(orderBook);
        document.body.appendChild(debugElement);
    }
})();
