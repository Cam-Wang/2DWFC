using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class Module : ScriptableObject
{
    public ModuleIDS id;
    public ModuleIDS[] upConnections;
    public ModuleIDS[] downConnections;
    public ModuleIDS[] rightConnections;
    public ModuleIDS[] leftConnections;
    public Tile tile;
}
public enum ModuleIDS
{
    Grass,
    Sand,
    Tree,
    Water,
    VerticalRoad,
    HorizontalRoad,
    BottomLeftRoad,
    BottomRightRoad,
    TopLeftRoad,
    TopRightRoad

}
