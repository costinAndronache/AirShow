"use strict";
require("../PDFJS.d.ts");
var ViewPresentationHelper = (function () {
    function ViewPresentationHelper(message) {
        this.message = message;
    }
    ViewPresentationHelper.prototype.greet = function () {
        alert(this.message);
    };
    return ViewPresentationHelper;
}());
window.onload = function (ev) {
    var greeter = new ViewPresentationHelper("Hello");
    greeter.greet();
};
//# sourceMappingURL=ViewPresentation.js.map