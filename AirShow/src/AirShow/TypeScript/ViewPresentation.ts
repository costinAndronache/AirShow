
///<reference path="Definitions/PDFJS.d.ts"/>

class ViewPresentationHelper {

    pdfURL: string;
    canvasId: string;
    canvas: HTMLCanvasElement;
    pdfFile: PDFDocumentProxy;
    currentDisplayedPage: number;
    constructor(pdfURL: string, canvasId: string) {
        this.pdfURL = pdfURL;
        this.canvasId = canvasId;
        this.currentDisplayedPage = -1;
        this.canvas = document.getElementById(this.canvasId) as HTMLCanvasElement;
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
   
            var context = self.canvas.getContext('2d');
   
            var viewport = page.getViewport(1);

            viewport = page.getViewport(1.0);

            self.canvas.height = viewport.height;
            self.canvas.width = viewport.width;

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
        var fullScreenButton = document.getElementById("fullScreenButton") as HTMLButtonElement;

        var self = this;

        fullScreenButton.onclick = function (ev: Event) {
            if (document.fullscreenElement) {
                if (document.exitFullscreen) {
                    document.exitFullscreen();
                }
            } else {
                if (!self.makeCanvasFullScreen())
                    alert('This browser does not have full-screen capabilities');
            }
        }

        window.addEventListener('keydown', function (ev: KeyboardEvent) {
            if (ev.keyCode == 37) { // left arrow 
                self.displayPreviousPage();
            }

            if (ev.keyCode == 39) { // right arrow
                self.displayNextPage();
            }

        });
    }



    private makeCanvasFullScreen(): boolean {
        var cv = this.canvas;

        var adjustWidthHeight = function () {
            cv.style.height = "100%";
            cv.style.width = "100%";
        }
        if (cv.requestFullscreen) {
            cv.requestFullscreen();
            adjustWidthHeight();
            return true
        }

        if (cv.webkitRequestFullScreen) {
            cv.webkitRequestFullScreen();
            adjustWidthHeight();
            return true
        }


        return false;

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

window.addEventListener("load", function () {
    let greeter = new ViewPresentationHelper(window["presentationURL"], "pdfHost");
    let activationHelper = new ActivationHelper(greeter);
    greeter.run();
    activationHelper.run();
    window["activationHelper"] = activationHelper;
});

