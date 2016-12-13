
///<reference path="Definitions/PDFJS.d.ts"/>

class ViewPresentationHelper {

    message: string;
    constructor(message: string) {
        this.message = message
    }

    public greet() {
        alert(this.message);
    }
}



window.onload = function (ev: Event) {
    let greeter = new ViewPresentationHelper("Hello");
    greeter.greet();
};
