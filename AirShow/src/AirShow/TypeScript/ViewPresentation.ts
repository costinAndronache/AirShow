
///<reference path="Definitions/PDFJS.d.ts"/>

class ViewPresentationHelper {

    pdfURL: string;
    canvasId: string;
    pdfFile: PDFDocumentProxy;
    currentDisplayedPage: number;
    constructor(pdfURL: string, canvasId: string) {
        this.pdfURL = pdfURL;
        this.canvasId = canvasId;
        this.currentDisplayedPage = -1;
    }



    run() {
        var self = this;
        PDFJS.workerSrc = "/lib/pdfjs-dist/build/pdf.worker.min.js";
        PDFJS.getDocument(this.pdfURL).then(function (pdfPromise: PDFDocumentProxy) {
            self.pdfFile = pdfPromise;
            self.nextStepAfterLoadingPDF();
          
        });
    }


    private nextStepAfterLoadingPDF() {
        this.displayPage(1);
        this.setupControls();
    }


    private displayPage(index: number) {
        var self = this;
        this.pdfFile.getPage(index).then(function (page: PDFPageProxy) {
            
            var canvas: HTMLCanvasElement = document.getElementById(self.canvasId) as HTMLCanvasElement;
            var context = canvas.getContext('2d');

            var viewport = page.getViewport(1);
            var diff = window.screen.height - 2 * absoluteY(canvas);
            var scale = (diff) / viewport.height;
            console.log(diff + ", " + viewport.height + ", " + scale);
            viewport = page.getViewport(0.7);

            canvas.height = viewport.height;
            canvas.width = viewport.width;

            var renderContext = {
                canvasContext: context,
                viewport: viewport
            };
            page.render(renderContext);
            self.currentDisplayedPage = index;
        });
    }

    displayNextPage() {
        if (this.currentDisplayedPage < this.pdfFile.numPages) {
            this.displayPage(this.currentDisplayedPage + 1);
        }
    }

    displayPreviousPage() {
        if (this.currentDisplayedPage > 1) {
            this.displayPage(this.currentDisplayedPage - 1);
        }
    }

    private setupControls() {
        var previousButton = document.getElementById("previousButton") as HTMLButtonElement;
        var nextButton = document.getElementById("nextButton") as HTMLButtonElement;
        var self = this;
        previousButton.onclick = function (ev: Event) {
            self.displayPreviousPage();
        };

        nextButton.onclick = function (ev: Event) {
            self.displayNextPage();
        }
    }

}

class PresentationControllerHelper {

    private presentationHelper: ViewPresentationHelper;
    private connectionString: string;
    private ws: WebSocket;

    constructor(connectionString: string, presentationHelper: ViewPresentationHelper) {
        this.connectionString = connectionString;
        this.presentationHelper = presentationHelper;
    }


    run() {
        var self = this;
        this.ws = new WebSocket("ws://" + location.host);
        this.ws.onopen = function (ev: Event) {
            self.ws.send(window["activationRequestString"]);
        };

        this.ws.onerror = function (ev: Event) {
            alert("There was an error though");
        };

        this.ws.onclose = function (ev: CloseEvent) {
            alert("Again closed " + ev.code);
        }

        this.ws.onmessage = function (ev: MessageEvent) {
            var message = JSON.parse(ev.data);
            self.handleMessage(message);
        };
    }

     handleMessage(message: any) {
        var messageCode = message[kActionTypeCodeKey] as number;
        if (messageCode == ActionTypeCode.PageChangeAction)
        {
            this.handleChangePageMessage(message);
        }
    }

    private handleChangePageMessage(message: any) {
        var changePageTypeCode = message[kPageChangeActionTypeKey] as number;
        if (changePageTypeCode == PageChangeActionType.MoveNext) {
            this.presentationHelper.displayNextPage();
        } else {
            this.presentationHelper.displayPreviousPage();
            
        }
    }

}


class ActivationHelper {

    private presentationHelper: ViewPresentationHelper
    private controllerHelper: PresentationControllerHelper
    constructor(presentationHelper: ViewPresentationHelper) {
        this.presentationHelper = presentationHelper;
    }

    run() {

        var self = this;
        var activateButton = document.getElementById("activateButton") as HTMLButtonElement;
        activateButton.onclick = function (ev: Event) {
            self.controllerHelper = new PresentationControllerHelper(window["activationRequestString"], self.presentationHelper);
            activateButton.hidden = true;
            self.controllerHelper.run();
        }
    }
}

window.onload = function (ev: Event) {
    let greeter = new ViewPresentationHelper(window["presentationURL"], "pdfHost");
    let activationHelper = new ActivationHelper(greeter);
    greeter.run();
    activationHelper.run();
    window["activationHelper"] = activationHelper;

};
