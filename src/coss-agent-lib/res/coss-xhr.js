(function () {
    var debugId = "{debugId}";
    var url = "{url}";        

    var ajax = new XMLHttpRequest();
    ajax.onreadystatechange = function () {
        if (ajax.readyState === 4 && ajax.status === 200) {
            var responseDiv = document.createElement('div');
            responseDiv.id = "{debugId}";
            responseDiv.innerText = ajax.responseText;
            document.body.appendChild(responseDiv);
        }
    };

    ajax.open("GET", url);
    ajax.send();
})();