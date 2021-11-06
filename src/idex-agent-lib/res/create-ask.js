// symbol = '[SYMBOL]';
var symbol = 'AURA';

var regionB = document.getElementsByClassName("layout--exchange-region-b")[0];
// var panels = regionB.getElementsByClassName("component panel");
var transactionPanels = document.getElementsByClassName("transaction");

_.firstOrDefault = function (collection, predicate) {
    if (collection === null || collection.length === 0) { return null; }

    if (predicate === undefined || predicate === null) {
        return collection[0];
    }

    var results = _.filter(collection, predicate);
    if (results === null || results.length === 0) { return null; }
    return results[0];
}

_.singleOrDefault = function (collection, predicate) {
    if (collection === null || collection.length === 0) { return null; }

    if (predicate === undefined || predicate === null) {
        if (collection.length !== 1) { throw "Collection contains more than one element";}
        return collection[0];
    }

    var results = _.filter(collection, predicate);
    if (results === null || results.length === 0) { return null; }
    if (results.length !== 1) { throw "Collection contains more than one element"; }
    return results[0];
}

var getPanelControls = function (panel) {
    var grid = _.singleOrDefault(panel.getElementsByClassName("grid"));
    var gridDivs = grid.getElementsByTagName("div");
    console.log('Found ' + gridDivs.length + ' divs in the grid.');

    if (gridDivs === null || gridDivs.length !== 18) { throw "Wrong number of divs found."; }
    var priceDiv = gridDivs[4];
    var ethDiv = gridDivs[5];
    if (ethDiv.innerText !== "ETH") { throw "Expected to find \"ETH\" in div 5 but did not."; }   

    var priceInput = _.singleOrDefault(priceDiv.getElementsByTagName("input"));

    var quantityDiv = gridDivs[7];
    var symbolDiv = gridDivs[8];
    if (symbolDiv.innerText !== symbol.toUpperCase()) { throw 'Expected to find "' + symbol.toUpperCase() + '" in div 5 but did not.'; }

    var quantityInput = _.singleOrDefault(quantityDiv.getElementsByTagName("input"));

    var totalDiv = gridDivs[10];
    var totalInput = _.singleOrDefault(totalDiv.getElementsByTagName("input"));

    // <a href="#" class="ui--button">Buy</a>
    var buyButton = _.singleOrDefault(grid.getElementsByTagName("a"), function (item) {
        return item.getAttribute("class") === 'ui--button'
            && item.innerText !== null && item.innerText.trim().toUpperCase() === "BUY";
        // return item.innerText !== null && item.innerText.trim().toUpperCase() === "BUY";
    })

    return {
        grid: grid,
        gridDivs: gridDivs,
        ethDiv: ethDiv,
        priceDiv: priceDiv,
        priceInput: priceInput,
        quantityInput: quantityInput,
        buyButton: buyButton,
        totalInput: totalInput
    };
}

var getBidOrAskPanel = function (isBid) {
    return _.singleOrDefault(transactionPanels, function (queryPanel) {
        var panelDivs = queryPanel.getElementsByTagName("div");
        return _.some(panelDivs, function (queryPanelDiv) {
            return queryPanelDiv.innerText !== null
                && queryPanelDiv.innerText.trim().toUpperCase().indexOf(isBid ? "BUY" : "SELL") === 0;
        });
    });
} 

var bidPanel = getBidOrAskPanel(true);

var askPanel = getBidOrAskPanel(false);

var bidPanelControls = getPanelControls(bidPanel);

bidPanelControls;

var getBidQuantity = function () { return bidPanelControls.quantityInput.value; }
var setBidQuantity = function (quantity) { bidPanelControls.quantityInput.value = quantity; };

var getBidPrice = function () { return bidPanelControls.priceInput.value; }
var setBidPrice = function (price) { bidPanelControls.priceInput.value = price; };

var clearBidQuantity = function () { bidPanelControls.quantityInput.value = null; }

var clearBidPrice = function () { bidPanelControls.priceInput.value = null; }

var placeBid = function (price, quantity) {
    setBidPrice(price);
    setBidQuantity(quantity);

    // bidPanelControls.grid.focus();
    bidPanelControls.totalInput.focus();

    var priceFromForm = getBidPrice();
    var quantityFromForm = getBidQuantity();

    if (priceFromForm != price) {
        console.log('Price did not persist.');
        return;
    }

    if (quantityFromForm != quantity) {
        console.log('Quantity did not persist.');
        return;
    }

    bidPanelControls.totalInput.focus();

    console.log("About to place bid.");
};

// placeBid(0.00030021, 503);