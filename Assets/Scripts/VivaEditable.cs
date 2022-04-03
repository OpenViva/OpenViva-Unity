using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Jobs;



namespace viva{

public abstract class VivaEditable: VivaObject{

    public VivaEditable( ImportRequest __internalSourceRequest ):base(__internalSourceRequest){
    }

    public virtual void OnCreateMenuSelected(){}
    public virtual void OnCreateMenuDeselected(){}
    public abstract string GetInfoHeaderTitleText();
    public abstract string GetInfoHeaderText();
    public abstract string GetInfoBodyContentText();
    public virtual void OnShare(){}
    public virtual void OnInstall( string subFolder=null ){}
    public virtual List<CreateMenu.OptionInfo> OnCreateMenuOptionInfoDrawer(){ return null; }
    public virtual List<CreateMenu.MultiChoiceInfo> OnCreateMultiChoiceInfoDrawer(){ return null; }
    public virtual CreateMenu.InputInfo[] OnCreateMenuInputInfoDrawer(){ return null; }
}

}