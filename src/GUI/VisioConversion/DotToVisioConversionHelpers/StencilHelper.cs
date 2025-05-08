using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Common;
using Domain;
using GUI.Common;
using Microsoft.Office.Interop.Visio;
using Path = System.IO.Path;

namespace GUI.VisioConversion.DotToVisioConversionHelpers;

public static class StencilHelper
{
    // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
    public static Document OpenStencil(VisOpenSaveArgs mode = VisOpenSaveArgs.visOpenRO | VisOpenSaveArgs.visOpenHidden)
    {
        foreach (Window win in SharedGui.MyVisioApp.Windows)
        {
            if (win.Document.Name == SharedConstants.STENCILNAME)
            {
                return win.Document;
            }
        }

        var filePath = Path.Combine(PathUtils.LocalDataDirectory(), SharedConstants.STENCILNAME);

        if (!File.Exists(filePath))
        {
            Assembly asm = Assembly.GetExecutingAssembly();

            string[] resNames = asm.GetManifestResourceNames();
            var grammarFileRes = resNames.First(res => res.EndsWith(SharedConstants.STENCILNAME));

            Stream grammarFileResStream = asm.GetManifestResourceStream(grammarFileRes);

            using (var fileStream = File.Create(filePath))
            {
                grammarFileResStream.CopyTo(fileStream);
            }
        }

        try
        {
            var stencil = SharedGui.MyVisioApp.Documents.OpenEx(filePath, (short)mode);
            // Make sure master shapes have no layers
            foreach (Master master in stencil.Masters)
            {
                while (master.Layers.Count > 0)
                {
                    master.Layers[1].Delete(0);
                }
            }

            return stencil;
        }
        catch (Exception e)
        {
            throw new DotVisioException("Couldn't open Visio Stencil " + filePath + ": " + e.Message);
        }
    }
}