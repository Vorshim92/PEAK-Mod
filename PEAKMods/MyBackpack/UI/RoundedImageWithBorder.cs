using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[AddComponentMenu("UI/Rounded Image With Border")]
public class RoundedImageWithBorder : Image
{
    [Header("Shape Properties")]
    public float cornerRadius = 15f;
    public float borderWidth = 2f;
    [Range(3, 40)] public int cornerSegments = 16;
    
    // NOTA: La proprietà 'borderColor' è stata rimossa.
    // Il 'color' del componente Image controllerà ora il colore dell'anello.

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        
        float effectiveBorderWidth = Mathf.Max(0, borderWidth);
        if (effectiveBorderWidth <= 0) return; // Se non c'è bordo, non disegna nulla

        float outerRadius = Mathf.Max(0, cornerRadius);
        float innerRadius = Mathf.Max(0, cornerRadius - effectiveBorderWidth);
        
        // Calcola il rettangolo interno
        Rect innerRect = new Rect(
            rect.x + effectiveBorderWidth,
            rect.y + effectiveBorderWidth,
            rect.width - effectiveBorderWidth * 2,
            rect.height - effectiveBorderWidth * 2
        );

        if (innerRect.width < 0 || innerRect.height < 0) return;

        // --- 1. Genera i vertici del contorno esterno e interno ---
        List<Vector2> outerVerts = GenerateContour(rect, outerRadius);
        List<Vector2> innerVerts = GenerateContour(innerRect, innerRadius);

        // --- 2. Aggiungi i vertici alla mesh ---
        int startVertIndex = vh.currentVertCount;
        for (int i = 0; i < outerVerts.Count; i++)
        {
            vh.AddVert((Vector3)outerVerts[i], color, Vector2.zero);
        }
        for (int i = 0; i < innerVerts.Count; i++)
        {
            vh.AddVert((Vector3)innerVerts[i], color, Vector2.zero);
        }

        // --- 3. Crea i triangoli per formare l'anello ---
        int outerVertCount = outerVerts.Count;
        for (int i = 0; i < outerVertCount; i++)
        {
            int i0 = startVertIndex + i;
            int i1 = startVertIndex + ((i + 1) % outerVertCount);
            int i2 = startVertIndex + outerVertCount + i;
            int i3 = startVertIndex + outerVertCount + ((i + 1) % outerVertCount);
            
            vh.AddTriangle(i0, i1, i3);
            vh.AddTriangle(i0, i3, i2);
        }
    }

    private List<Vector2> GenerateContour(Rect rect, float radius)
    {
        List<Vector2> contour = new List<Vector2>();
        
        // Assicurati che il raggio non sia troppo grande
        radius = Mathf.Min(radius, rect.width / 2, rect.height / 2);

        // Angolo in alto a destra
        Vector2 trCenter = new Vector2(rect.xMax - radius, rect.yMax - radius);
        for (int i = 0; i <= cornerSegments; i++)
            contour.Add(trCenter + new Vector2(Mathf.Cos(Mathf.Deg2Rad * (90f / cornerSegments * i)), Mathf.Sin(Mathf.Deg2Rad * (90f / cornerSegments * i))) * radius);
        
        // Angolo in alto a sinistra
        Vector2 tlCenter = new Vector2(rect.xMin + radius, rect.yMax - radius);
        for (int i = 0; i <= cornerSegments; i++)
            contour.Add(tlCenter + new Vector2(Mathf.Cos(Mathf.Deg2Rad * (90 + 90f / cornerSegments * i)), Mathf.Sin(Mathf.Deg2Rad * (90 + 90f / cornerSegments * i))) * radius);

        // Angolo in basso a sinistra
        Vector2 blCenter = new Vector2(rect.xMin + radius, rect.yMin + radius);
        for (int i = 0; i <= cornerSegments; i++)
            contour.Add(blCenter + new Vector2(Mathf.Cos(Mathf.Deg2Rad * (180 + 90f / cornerSegments * i)), Mathf.Sin(Mathf.Deg2Rad * (180 + 90f / cornerSegments * i))) * radius);

        // Angolo in basso a destra
        Vector2 brCenter = new Vector2(rect.xMax - radius, rect.yMin + radius);
        for (int i = 0; i <= cornerSegments; i++)
            contour.Add(brCenter + new Vector2(Mathf.Cos(Mathf.Deg2Rad * (270 + 90f / cornerSegments * i)), Mathf.Sin(Mathf.Deg2Rad * (270 + 90f / cornerSegments * i))) * radius);

        return contour;
    }
}