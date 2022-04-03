using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace viva{


public class BlackWhiteList : MonoBehaviour{

    public delegate void StringListReturnFunc( List<string> result );
    
    [SerializeField]
    private ScrollViewManager whiteScroll;
    [SerializeField]
    private ScrollViewManager blackScroll;
    [SerializeField]
    private Button addButton;
    [SerializeField]
    private Button removeButton;
    [SerializeField]
    private Text instruction1;
    [SerializeField]
    private Text instruction2;

    public List<string> whitelist { get; private set; }
    private List<string> blacklist;
    private int? selected = null;


    public void SetupBlackWhiteList( string _instruction1, string _instruction2, List<string> _whitelist, List<string> _blacklist ){

        instruction1.text = _instruction1;
        instruction2.text = _instruction2;
        whitelist = _whitelist;
        blacklist = _blacklist;

        DisplayLists();
    }

    private void DisplayLists(){
        int index = 0;
        whiteScroll.SetContentCount( whitelist.Count, delegate( Transform prefabEntry ){

            Button button = prefabEntry.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            Text title = prefabEntry.GetChild(0).GetComponent<Text>();

            int current = index;
            title.text = whitelist[ index++ ];
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener( delegate{ SelectForRemoval( current ); } );
        } );
        index = 0;
        blackScroll.SetContentCount( blacklist.Count, delegate( Transform prefabEntry ){
            Button button = prefabEntry.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            Text title = prefabEntry.GetChild(0).GetComponent<Text>();

            int current = index;
            title.text = blacklist[ index++ ];
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener( delegate{ SelectForAddition( current ); } );
        } );
    }

    private void SelectForRemoval( int index ){
        addButton.interactable = false;
        removeButton.interactable = true;
        selected = index;

    }
    private void SelectForAddition( int index ){
        addButton.interactable = true;
        removeButton.interactable = false;
        selected = index;
    }

    public void Add(){
        if( !selected.HasValue ) return;

        var toAdd = blacklist[ selected.Value ];
        blacklist.RemoveAt( selected.Value );
        whitelist.Add( toAdd );

        DisplayLists();
        addButton.interactable = selected.Value < blacklist.Count;
    }

    public void Remove(){
        if( !selected.HasValue ) return;

        var toRemove = whitelist[ selected.Value ];
        whitelist.RemoveAt( selected.Value );
        blacklist.Add( toRemove );

        DisplayLists();
        addButton.interactable = selected.Value < whitelist.Count;
    }
}

}