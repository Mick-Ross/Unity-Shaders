using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;

[ExecuteInEditMode]
public class changeLensRadius : MonoBehaviour
{
    float poleHeightMult;
    float parentYScale;
    [SerializeField] [Range(0.1f, 4.5f)]
    float lensScaleMult;
    [SerializeField]
    Transform lensTransform, poleTransform, attachPoint, lensParent;

    const float defPoleYScale = 35;

    void Start() {
        // this is the default value set on the slider adjusted
        setLensRadius(0.98f);
    }

    // Update is called once per frame
    void Update() {
        setLensRadius(lensScaleMult);
        parentYScale = lensParent.localScale.y;
        Debug.Log("Parent Lens Scale: " + parentYScale);
        //Debug.Log("Attach pt. local scale: " + calculateDesiredPoleLength());
    }

    void setLensRadius(float scale) {
        lensScaleMult = scale;
        lensTransform.localScale = new Vector3(lensScaleMult, 1, lensScaleMult);
        setPoleHeight();
    }

    void setPoleHeight() {
        float attachPt = calculateDesiredPoleLength();

        /*  
         *  here we divide by 2 because when I imported the asset from Blender to Unity,
         *  the scale was 0.35, but the total height is double that, since I moved the entire object 
         *  above the origin instead of the pivot being placed in the center of the pole
        */
        float scale2Transform = attachPt * defPoleYScale / 2f * (1 / parentYScale);
        poleTransform.localScale = new Vector3(1, 1, scale2Transform);

    }

    float calculateDesiredPoleLength() {
        // Debug.Log($"{poleTransform.InverseTransformPoint(attachPoint.position).y - poleTransform.localPosition.y}");
        // return poleTransform.InverseTransformPoint(attachPoint.position).y - poleTransform.localPosition.y;

        // Vector3 displacement = attachPoint.position - poleTransform.position;
        // displacement = lensTransform.InverseTransformVector(displacement);

        //Debug.Log($"{displacement.magnitude}");
        // return displacement.magnitude;

        return attachPoint.position.y - poleTransform.position.y;
    }

    // linearly transforming the slider values to our constraints on the range of the lens multiplier
    public void OnSliderUpdated(SliderEventData eventData) {
        setLensRadius(4.4f * (float)(eventData.NewValue) + 0.1f);
    }
    
}
