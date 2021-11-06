// symbol = '[SYMBOL]';
// var symbol = 'AURA';

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

var getPanelControls = function (panel, isBid) {
    var grid = _.singleOrDefault(panel.getElementsByClassName("grid"));
    var gridDivs = grid.getElementsByTagName("div");
    console.log('Found ' + gridDivs.length + ' divs in the grid.');

    if (gridDivs === null || gridDivs.length !== 18) { throw "Wrong number of divs found."; }
    var priceDiv = gridDivs[4];
    var ethDiv = gridDivs[5];
    if (ethDiv.innerText !== "ETH") { throw "Expected to find \"ETH\" in div 5 but did not."; }   

    var priceInput = _.singleOrDefault(priceDiv.getElementsByTagName("input"));

    var amountAnchorDiv = gridDivs[6];
    var amountAnchor = _.singleOrDefault(amountAnchorDiv.getElementsByTagName("a"));

    var quantityDiv = gridDivs[7];
    var symbolDiv = gridDivs[8];

    var quantityInput = _.singleOrDefault(quantityDiv.getElementsByTagName("input"));

    var totalDiv = gridDivs[10];
    var totalInput = _.singleOrDefault(totalDiv.getElementsByTagName("input"));

    var actionButton = _.singleOrDefault(grid.getElementsByTagName("a"), function (item) {
        return item.getAttribute("class") === 'componentBasicButton' //'ui--button'
            && item.innerText !== null && item.innerText.trim().toUpperCase() === (isBid ? "BUY" : "SELL");
    });
    
    return {
        grid: grid,
        gridDivs: gridDivs,
        symbolDiv: symbolDiv,
        ethDiv: ethDiv,
        amountAnchor: amountAnchor,
        priceDiv: priceDiv,
        priceInput: priceInput,
        quantityInput: quantityInput,
        actionButton: actionButton,
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

var bidPanelControls = getPanelControls(bidPanel, true);
var askPanelControls = getPanelControls(askPanel, false);

bidPanelControls.priceInput.id = 'selenium_bidPriceInput';
bidPanelControls.quantityInput.id = 'selenium_bidQuantityInput';
bidPanelControls.symbolDiv.id = 'selenium_bidSymbolDiv';
bidPanelControls.amountAnchor.id = 'selenium_bidAmountAnchor';
bidPanelControls.actionButton.id = 'selenium_buyButton';

askPanelControls.priceInput.id = 'selenium_askPriceInput';
askPanelControls.quantityInput.id = 'selenium_askQuantityInput';
askPanelControls.symbolDiv.id = 'selenium_askSymbolDiv';
askPanelControls.amountAnchor.id = 'selenium_askAmountAnchor';
askPanelControls.actionButton.id = 'selenium_sellButton';
