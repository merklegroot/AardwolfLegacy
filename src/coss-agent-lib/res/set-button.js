(function () {    
    var linky = document.createElement('a');
    linky.href = 'https://chrome.google.com/webstore/detail/editthiscookie/fngmhnnpilhplaeedifhccceomclgfbg';
    linky.innerText = 'Edit this cookie chrome add on';
    linky.target = '_blank';

    document.body.appendChild(linky);

    var lineBreak = document.createElement('br');
    document.body.appendChild(lineBreak);

    var button = document.createElement('button');
    button.id = 'debugButton';
    button.innerText = 'Click after setting cookies.';
    button.onclick = function () {
        document.body.removeChild(linky);
        document.body.removeChild(lineBreak);
        document.body.removeChild(button);
    };

    //document.body.appendChild(document.createElement('br'));
    //document.body.appendChild(document.createElement('br'));
    //document.body.appendChild(document.createElement('br'));

    //var cookieTextBox = document.createElement('input');
    //cookieTextBox.id = 'cookieTextBox';
    //document.body.appendChild(cookieTextBox);

    //document.body.appendChild(document.createElement('br'));

    //var cookieButton = document.createElement('button');
    //cookieButton.id = 'cookieButton';
    //cookieButton.innerText = 'Click here to set cookies in the text box.';
    //cookieButton.onclick = function () {
    //    document.body.removeChild(cookieButton);
    //};

    document.body.appendChild(button);
})();