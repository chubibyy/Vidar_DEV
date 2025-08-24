using System;

[Serializable]
public class BoardState
{
    public int turnIndex;        // 0,1,2...
    public int activePlayer;     // 0 = J1, 1 = J2
    public int movesP1;          // compteur de coups (exemple)
    public int movesP2;

    public static BoardState CreateInitial()
    {
        return new BoardState { turnIndex = 0, activePlayer = 0, movesP1 = 0, movesP2 = 0 };
    }
}
