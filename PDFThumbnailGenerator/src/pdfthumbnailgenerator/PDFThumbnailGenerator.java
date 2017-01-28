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
        if (args.length < 3) {
            System.out.println("Usage: pdfPath jpegOutputPath");
            System.exit(-1);
        }
      
      File file = new File(args[1]);
      try {
            try (PDDocument document = PDDocument.load(file)) {
                PDFRenderer renderer = new PDFRenderer(document);                
                BufferedImage image = renderer.renderImage(0);
                ImageIO.write(image, "JPEG", new File(args[2]));
                document.close();
                System.exit(0);
            }
      } catch(Exception ex) 
        {
            System.exit(-1);
        }
    }
    
}
