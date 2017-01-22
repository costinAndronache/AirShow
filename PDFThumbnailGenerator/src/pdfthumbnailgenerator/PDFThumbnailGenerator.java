/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package pdfthumbnailgenerator;
import java.awt.image.BufferedImage;
import java.io.File;

import javax.imageio.ImageIO;
import org.apache.pdfbox.pdmodel.PDDocument;
import org.apache.pdfbox.rendering.PDFRenderer;


/**
 *
 * @author Costi
 */
public class PDFThumbnailGenerator {

    /**
     * @param args the command line arguments
     */
    public static void main(String[] args) {
         //Loading an existing PDF document
      
        if (args.length < 3) {
            System.out.println("Usage: program pdfPath jpegOutputPath");
            System.exit(-1);
        }
      
      File file = new File(args[1]);
      try {
          
      
            //Instantiating the PDFRenderer class
            try (PDDocument document = PDDocument.load(file)) {
                //Instantiating the PDFRenderer class
                PDFRenderer renderer = new PDFRenderer(document);
                
                //Rendering an image from the PDF document
                BufferedImage image = renderer.renderImage(0);
                
                //Writing the image to a file
                ImageIO.write(image, "JPEG", new File(args[2]));
                
                System.exit(0);
                
                //Closing the document
            }
      } catch(Exception ex) 
        {
            System.exit(-1);
        }
    }
    
}
