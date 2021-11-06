(function () {
    var feedbackId = '[FEEDBACK_ID]';
    var rowId = '[ROW_ID]';

    var setDebugText = function (text) {
        var debugElement = document.getElementById(feedbackId);
        if (debugElement !== null) {
            debugElement.innerText = text;
        } else {
            debugElement = document.createElement('div');
            debugElement.id = feedbackId;
            debugElement.innerText = text;
            document.body.appendChild(debugElement);
        }
    }

    var row = document.getElementById(rowId);
    var cells = null;
    if (row !== null) {
        cells = row.getElementsByTagName("td");
    }

    var cancelAnchor = null;
    if (cells !== null && cells.length === 6) {
        var cancelCell = cells[5];
        var anchors = cancelCell.getElementsByTagName("a");
        if (anchors !== null && anchors.length === 1) {
            cancelAnchor = anchors[0];
        }
    }

    if (cancelAnchor !== null) {
        cancelAnchor.click();
        setDebugText(JSON.stringify({ wasSuccessful: true }));
    } else {
        setDebugText(JSON.stringify({ wasSuccessful: false, reason: "anchor not found." }));
    }
})();