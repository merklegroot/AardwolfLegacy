(function () {
    var button = _.singleOrDefault(document.getElementsByClassName("ui--button"), function (item) { return item.innerText !== null && item.innerText.toUpperCase() === 'OK' });

    if (button !== null) {
        button.click();
    }
})();