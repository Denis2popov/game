using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    public int width;
    public int height;
    
    public GameObject tilePrefab;
    public GameObject[] gamePiecePrefabs;
    
    private GameObject[,] tiles;
    private GamePiece[,] gamePieces;
    
    public float swapTime = 0.5f;
    public float fallTime = 0.3f;
    public float fillTime = 0.2f;
    
    private GamePiece selectedPiece;
    private bool isSwapping = false;
    private bool isFilling = false;
    
    // Добавляем переменные для системы подсказок
    private float hintDelay = 5f; // Время ожидания перед показом подсказки
    private float hintTimer = 0f;
    private bool showingHint = false;
    private GamePiece[] hintPieces = new GamePiece[2]; // Для хранения фигур подсказки
    
    void Start()
    {
        // Проверки на корректность входных данных
        if (tilePrefab == null)
        {
            Debug.LogError("tilePrefab не назначен! Пожалуйста, назначьте префаб в инспекторе.");
            return;
        }
        
        if (gamePiecePrefabs == null || gamePiecePrefabs.Length == 0)
        {
            Debug.LogError("gamePiecePrefabs пуст! Пожалуйста, добавьте префабы фигур в инспекторе.");
            return;
        }
        
        foreach (GameObject prefab in gamePiecePrefabs)
        {
            if (prefab == null)
            {
                Debug.LogError("Один из префабов в gamePiecePrefabs не назначен! Проверьте массив в инспекторе.");
                return;
            }
            
            if (prefab.GetComponent<GamePiece>() == null)
            {
                Debug.LogError("Префаб " + prefab.name + " не содержит компонент GamePiece! Добавьте компонент GamePiece к этому префабу.");
                return;
            }
        }
        
        tiles = new GameObject[width, height];
        gamePieces = new GamePiece[width, height];
        
        SetupBoard();
        FillBoard();
    }
    
    void SetupBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 position = new Vector2(x, y);
                GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity);
                tile.transform.parent = transform;
                tile.name = "Tile (" + x + "," + y + ")";
                tiles[x, y] = tile;
            }
        }
    }
    
    void FillBoard()
    {
        // Сначала заполняем доску
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                SpawnGamePiece(x, y, true); // Добавляем параметр для проверки совпадений
            }
        }
    }
    
    void SpawnGamePiece(int x, int y, bool checkForMatches = false)
    {
        if (gamePiecePrefabs.Length == 0)
        {
            Debug.LogError("Нет префабов для создания!");
            return;
        }
        
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            Debug.LogError("Попытка создать фигуру за пределами доски: (" + x + "," + y + ")");
            return;
        }
        
        // Список запрещенных типов фигур (которые вызовут совпадение)
        List<int> forbiddenTypes = new List<int>();
        
        if (checkForMatches)
        {
            // Проверяем, какие типы вызовут горизонтальное совпадение
            if (x > 1) // Если есть две фигуры слева
            {
                GamePiece leftPiece1 = gamePieces[x-1, y];
                GamePiece leftPiece2 = gamePieces[x-2, y];
                if (leftPiece1 != null && leftPiece2 != null && leftPiece1.type == leftPiece2.type)
                {
                    forbiddenTypes.Add(leftPiece1.type);
                }
            }
            
            // Если есть фигура слева и фигура справа с одинаковым типом
            if (x > 0 && x < width - 1)
            {
                GamePiece leftPiece = gamePieces[x-1, y];
                GamePiece rightPiece = gamePieces[x+1, y];
                if (leftPiece != null && rightPiece != null && leftPiece.type == rightPiece.type)
                {
                    forbiddenTypes.Add(leftPiece.type);
                }
            }
            
            // Проверяем, какие типы вызовут вертикальное совпадение
            if (y > 1) // Если есть две фигуры снизу
            {
                GamePiece belowPiece1 = gamePieces[x, y-1];
                GamePiece belowPiece2 = gamePieces[x, y-2];
                if (belowPiece1 != null && belowPiece2 != null && belowPiece1.type == belowPiece2.type)
                {
                    forbiddenTypes.Add(belowPiece1.type);
                }
            }
        }
        
        // Выбираем случайный тип, исключая запрещенные
        int randomIndex;
        if (forbiddenTypes.Count >= gamePiecePrefabs.Length)
        {
            // Если все типы запрещены (маловероятно), выбираем просто случайный
            randomIndex = Random.Range(0, gamePiecePrefabs.Length);
        }
        else
        {
            do
            {
                randomIndex = Random.Range(0, gamePiecePrefabs.Length);
            }
            while (forbiddenTypes.Contains(randomIndex));
        }
        
        Vector2 position = new Vector2(x, y);
        
        GameObject piece = Instantiate(gamePiecePrefabs[randomIndex], position, Quaternion.identity);
        if (piece != null)
        {
            piece.transform.parent = transform;
            
            GamePiece gamePiece = piece.GetComponent<GamePiece>();
            if (gamePiece != null)
            {
                gamePieces[x, y] = gamePiece;
                gamePiece.Init(x, y);
                
                // Сохраняем тип из префаба
                gamePiece.type = randomIndex;
                
                piece.name = "Piece (" + x + "," + y + ")";
            }
            else
            {
                Debug.LogError("Префаб не содержит компонент GamePiece!");
                Destroy(piece);
            }
        }
        else
        {
            Debug.LogError("Не удалось создать фигуру!");
        }
    }
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isSwapping && !isFilling)
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int x = Mathf.RoundToInt(mousePosition.x);
            int y = Mathf.RoundToInt(mousePosition.y);
            
            if (IsInBounds(x, y))
            {
                SelectPiece(x, y);
                
                // Сбрасываем таймер подсказки при любом действии игрока
                hintTimer = 0f;
                // Убираем текущую подсказку, если она показывается
                if (showingHint)
                {
                    StopHint();
                }
            }
        }
        
        // Обработка таймера подсказки
        if (!isSwapping && !isFilling && !showingHint)
        {
            hintTimer += Time.deltaTime;
            
            if (hintTimer >= hintDelay)
            {
                ShowHint();
            }
        }
    }
    
    bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
    
    void SelectPiece(int x, int y)
    {
        GamePiece piece = gamePieces[x, y];
    
        if (piece == null)
        {
            Debug.LogWarning("Попытка выбрать несуществующую фигуру на позиции (" + x + "," + y + ")");
            return;
        }
    
        // Анимация при выборе фишки
        StartCoroutine(AnimatePieceSelection(piece));
    
        if (selectedPiece == null)
        {
            selectedPiece = piece;
        }
        else
        {
            if (IsAdjacent(selectedPiece.xIndex, selectedPiece.yIndex, x, y))
            {
                StartCoroutine(SwapPieces(selectedPiece, piece));
            }
            else
            {
                // Отменяем выделение первой фишки
                StartCoroutine(AnimatePieceDeselection(selectedPiece));
                selectedPiece = piece;
            }
        }
    }

    IEnumerator AnimatePieceSelection(GamePiece piece)
    {
        Vector3 originalScale = piece.transform.localScale;
        Vector3 selectedScale = originalScale * 1.2f;
    
        float duration = 0.2f;
        float elapsedTime = 0;
    
        // Увеличиваем размер
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            piece.transform.localScale = Vector3.Lerp(originalScale, selectedScale, EasingFunctions.EaseOutQuad(t));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    
        piece.transform.localScale = selectedScale;
    }

    IEnumerator AnimatePieceDeselection(GamePiece piece)
    {
        // Используем fixedScale из GamePiece
        Vector3 originalScale = piece.fixedScale;
        Vector3 currentScale = piece.transform.localScale;
        
        float duration = 0.2f;
        float elapsedTime = 0;
        
        // Возвращаем к исходному размеру
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            piece.transform.localScale = Vector3.Lerp(currentScale, originalScale, EasingFunctions.EaseOutQuad(t));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        piece.transform.localScale = originalScale;
    }
      
    bool IsAdjacent(int x1, int y1, int x2, int y2)
    {
        return (Mathf.Abs(x1 - x2) == 1 && y1 == y2) || (Mathf.Abs(y1 - y2) == 1 && x1 == x2);
    }
    
    IEnumerator SwapPieces(GamePiece piece1, GamePiece piece2)
    {
        isSwapping = true;
        
        // Сохраняем информацию о бомбах
        GamePiece.BombType bomb1Type = piece1.bombType;
        GamePiece.BombType bomb2Type = piece2.bombType;

        // Запомним индексы и типы
        int piece1X = piece1.xIndex;
        int piece1Y = piece1.yIndex;
        int piece2X = piece2.xIndex;
        int piece2Y = piece2.yIndex;
        int piece1Type = piece1.type;
        int piece2Type = piece2.type;

        // Меняем их местами в массиве
        gamePieces[piece1X, piece1Y] = piece2;
        gamePieces[piece2X, piece2Y] = piece1;

        // Обновляем индексы
        piece1.Init(piece2X, piece2Y);
        piece2.Init(piece1X, piece1Y);

        // Визуальная анимация
        float elapsedTime = 0;
        Vector2 piece1StartPosition = new Vector2(piece1X, piece1Y);
        Vector2 piece2StartPosition = new Vector2(piece2X, piece2Y);
        Vector2 centerPosition = (piece1StartPosition + piece2StartPosition) / 2;

        // Увеличиваем размер при начале свапа
        Vector3 originalScale = piece1.transform.localScale;
        Vector3 selectedScale = originalScale * 1.2f;

        while (elapsedTime < swapTime)
        {
            float t = elapsedTime / swapTime;
        
            // Используем функцию плавности
            float easedT = EasingFunctions.EaseInOutQuad(t);
        
            // Добавляем небольшую дугу в движении
            float height = 0.2f * Mathf.Sin(t * Mathf.PI);
        
            // Позиции с дугой
            Vector2 piece1Pos = Vector2.Lerp(piece1StartPosition, piece2StartPosition, easedT);
                        Vector2 piece2Pos = Vector2.Lerp(piece2StartPosition, piece1StartPosition, easedT);
        
            // Добавляем высоту для создания дуги
            piece1Pos.y += height;
            piece2Pos.y += height;
        
            // Изменяем размер во время движения
            float scaleFactor = 1 + 0.2f * Mathf.Sin(t * Mathf.PI);
            Vector3 currentScale = originalScale * scaleFactor;
        
            // Применяем позиции и масштаб
            piece1.transform.position = piece1Pos;
            piece2.transform.position = piece2Pos;
            piece1.transform.localScale = currentScale;
            piece2.transform.localScale = currentScale;
        
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Финальное положение и сброс масштаба
        piece1.transform.position = piece2StartPosition;
        piece2.transform.position = piece1StartPosition;
        piece1.transform.localScale = originalScale;
        piece2.transform.localScale = originalScale;

        // Проверка, активирована ли бомба
        List<GamePiece> bombedPieces = new List<GamePiece>();
        
        // Особая обработка для цветной бомбы
        if (bomb1Type == GamePiece.BombType.Color)
        {
            // Цветная бомба уничтожает все фигуры типа второй фигуры
            bombedPieces.AddRange(FindPiecesToBomb(piece1, bomb1Type, piece2Type));
            
            // Запускаем эффект цветной бомбы
            StartCoroutine(AnimateColorBombEffect(piece1, bombedPieces));
        }
        else if (bomb2Type == GamePiece.BombType.Color)
        {
            // Цветная бомба уничтожает все фигуры типа первой фигуры
            bombedPieces.AddRange(FindPiecesToBomb(piece2, bomb2Type, piece1Type));
            
            // Запускаем эффект цветной бомбы
            StartCoroutine(AnimateColorBombEffect(piece2, bombedPieces));
        }
        else if (bomb1Type != GamePiece.BombType.None || bomb2Type != GamePiece.BombType.None)
        {
            // Стандартная обработка линейных бомб
            if (bomb1Type != GamePiece.BombType.None)
            {
                bombedPieces.AddRange(FindPiecesToBomb(piece1, bomb1Type));
            }
            
            if (bomb2Type != GamePiece.BombType.None)
            {
                bombedPieces.AddRange(FindPiecesToBomb(piece2, bomb2Type));
            }
            
            // Удаляем дубликаты
            List<GamePiece> uniqueBombedPieces = new List<GamePiece>();
            foreach (GamePiece piece in bombedPieces)
            {
                if (!uniqueBombedPieces.Contains(piece))
                    uniqueBombedPieces.Add(piece);
            }
            
            ClearMatches(uniqueBombedPieces);
        }
        else
        {
            // Обычная проверка совпадений
            List<GamePiece> matches = FindMatches();
            if (matches.Count > 0)
            {
                ClearMatches(matches);
            }
            else
            {
                StartCoroutine(SwapPiecesBack(piece1, piece2));
            }
        }
    }
    
    
    IEnumerator SwapPiecesBack(GamePiece piece1, GamePiece piece2)
    {
        // То же самое, что SwapPieces, но без проверки совпадений в конце
        int piece1X = piece1.xIndex;
        int piece1Y = piece1.yIndex;
        int piece2X = piece2.xIndex;
        int piece2Y = piece2.yIndex;
        
        gamePieces[piece1X, piece1Y] = piece2;
        gamePieces[piece2X, piece2Y] = piece1;
        
        piece1.Init(piece2X, piece2Y);
        piece2.Init(piece1X, piece1Y);
        
        float elapsedTime = 0;
        Vector2 piece1StartPosition = new Vector2(piece1X, piece1Y);
        Vector2 piece2StartPosition = new Vector2(piece2X, piece2Y);
        
        while (elapsedTime < swapTime)
        {
            piece1.transform.position = Vector2.Lerp(piece1StartPosition, piece2StartPosition, elapsedTime / swapTime);
            piece2.transform.position = Vector2.Lerp(piece2StartPosition, piece1StartPosition, elapsedTime / swapTime);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        piece1.transform.position = piece2StartPosition;
        piece2.transform.position = piece1StartPosition;
        
        isSwapping = false;
        selectedPiece = null;
    }
    
    // Класс для хранения информации о совпадении
    public class MatchInfo
    {
        public List<GamePiece> pieces = new List<GamePiece>();
        public bool isHorizontal = false;
    }
    
    // Метод для поиска совпадений с дополнительной информацией
    List<MatchInfo> FindMatchesWithInfo()
    {
        List<MatchInfo> matchInfoList = new List<MatchInfo>();
        
        // Проверка горизонтальных совпадений
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 2; x++)
            {
                GamePiece piece1 = gamePieces[x, y];
                GamePiece piece2 = gamePieces[x + 1, y];
                GamePiece piece3 = gamePieces[x + 2, y];
                
                if (piece1 != null && piece2 != null && piece3 != null &&
                    piece1.type == piece2.type && piece2.type == piece3.type)
                {
                    // Нашли совпадение из 3+
                    MatchInfo info = new MatchInfo();
                    info.pieces.Add(piece1);
                    info.pieces.Add(piece2);
                    info.pieces.Add(piece3);
                    
                    // Проверяем, есть ли 4-е совпадение
                    if (x < width - 3)
                    {
                        GamePiece piece4 = gamePieces[x + 3, y];
                        if (piece4 != null && piece4.type == piece3.type)
                        {
                            info.pieces.Add(piece4);
                            
                            // Проверяем, есть ли 5-е совпадение
                            if (x < width - 4)
                            {
                                GamePiece piece5 = gamePieces[x + 4, y];
                                if (piece5 != null && piece5.type == piece4.type)
                                {
                                    info.pieces.Add(piece5);
                                }
                            }
                        }
                    }
                    
                    info.isHorizontal = true;
                    matchInfoList.Add(info);
                    
                    // Переходим к следующему потенциальному совпадению
                    x += info.pieces.Count - 1;
                }
            }
        }
        
        // Проверка вертикальных совпадений
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 2; y++)
            {
                GamePiece piece1 = gamePieces[x, y];
                GamePiece piece2 = gamePieces[x, y + 1];
                GamePiece piece3 = gamePieces[x, y + 2];
                
                if (piece1 != null && piece2 != null && piece3 != null &&
                    piece1.type == piece2.type && piece2.type == piece3.type)
                {
                    // Нашли совпадение из 3+
                    MatchInfo info = new MatchInfo();
                    info.pieces.Add(piece1);
                    info.pieces.Add(piece2);
                    info.pieces.Add(piece3);
                    
                    // Проверяем, есть ли 4-е совпадение
                    if (y < height - 3)
                    {
                        GamePiece piece4 = gamePieces[x, y + 3];
                        if (piece4 != null && piece4.type == piece3.type)
                        {
                            info.pieces.Add(piece4);
                            
                            // Проверяем, есть ли 5-е совпадение
                            if (y < height - 4)
                            {
                                GamePiece piece5 = gamePieces[x, y + 4];
                                if (piece5 != null && piece5.type == piece4.type)
                                {
                                    info.pieces.Add(piece5);
                                }
                            }
                        }
                    }
                    
                    info.isHorizontal = false;
                    matchInfoList.Add(info);
                    
                    // Переходим к следующему потенциальному совпадению
                    y += info.pieces.Count - 1;
                }
            }
        }
        
        return matchInfoList;
    }
    
    // Переопределяем метод FindMatches, чтобы он работал с оригинальным интерфейсом
    List<GamePiece> FindMatches()
    {
        List<MatchInfo> matchInfoList = FindMatchesWithInfo();
        List<GamePiece> allMatches = new List<GamePiece>();
        
        foreach (MatchInfo info in matchInfoList)
        {
            foreach (GamePiece piece in info.pieces)
            {
                if (!allMatches.Contains(piece))
                    allMatches.Add(piece);
            }
        }
        
        return allMatches;
    }
    
    // Метод для поиска фигур, которые должны быть уничтожены бомбой
    List<GamePiece> FindPiecesToBomb(GamePiece bombPiece, GamePiece.BombType bombType, int targetType = -1)
    {
        List<GamePiece> piecesToClear = new List<GamePiece>();
        
        // Добавляем саму бомбу в список на удаление
        piecesToClear.Add(bombPiece);
        
        if (bombType == GamePiece.BombType.Horizontal)
        {
            // Добавляем все фигуры в том же ряду
            for (int x = 0; x < width; x++)
            {
                GamePiece piece = gamePieces[x, bombPiece.yIndex];
                if (piece != null && piece != bombPiece)
                {
                    piecesToClear.Add(piece);
                }
            }
        }
        else if (bombType == GamePiece.BombType.Vertical)
        {
            // Добавляем все фигуры в том же столбце
            for (int y = 0; y < height; y++)
            {
                GamePiece piece = gamePieces[bombPiece.xIndex, y];
                if (piece != null && piece != bombPiece)
                {
                    piecesToClear.Add(piece);
                }
            }
        }
        else if (bombType == GamePiece.BombType.Color && targetType >= 0)
        {
            // Добавляем все фигуры указанного типа
            List<GamePiece> piecesOfType = FindPiecesOfType(targetType);
            piecesToClear.AddRange(piecesOfType);
        }
        
        return piecesToClear;
    }
    
    void ClearMatches(List<GamePiece> matches)
    {
        // Получаем подробную информацию о совпадениях
        List<MatchInfo> matchInfoList = FindMatchesWithInfo();
        
        // Создаем список для всех фигур, которые нужно уничтожить
        List<GamePiece> piecesToClear = new List<GamePiece>(matches);
        
        // Проверяем, нужно ли создать бомбу
        foreach (MatchInfo info in matchInfoList)
        {
            // Если в совпадении 5 или больше фигур, создаем цветную бомбу
            if (info.pieces.Count >= 5)
            {
                // Выбираем фигуру, которая станет бомбой
                GamePiece bombPiece = info.pieces[0];
                
                // Создаем цветную бомбу
                CreateBomb(bombPiece.xIndex, bombPiece.yIndex, bombPiece.type, GamePiece.BombType.Color);
                
                // Удаляем эту фигуру из списка на удаление
                piecesToClear.Remove(bombPiece);
            }
            // Если в совпадении 4 фигуры, создаем линейную бомбу
            else if (info.pieces.Count >= 4)
            {
                // Выбираем фигуру, которая станет бомбой
                GamePiece bombPiece = info.pieces[0];
                
                // Определяем тип бомбы (горизонтальная или вертикальная)
                GamePiece.BombType bombType = info.isHorizontal ? 
                    GamePiece.BombType.Horizontal : GamePiece.BombType.Vertical;
                
                // Создаем бомбу
                CreateBomb(bombPiece.xIndex, bombPiece.yIndex, bombPiece.type, bombType);
                
                // Удаляем эту фигуру из списка на удаление
                piecesToClear.Remove(bombPiece);
            }
        }
        
        // Анимируем и удаляем оставшиеся совпадения
        StartCoroutine(ClearMatchesAnimation(piecesToClear));
    }

    List<GamePiece> FindPiecesOfType(int pieceType)
        {
        List<GamePiece> piecesOfType = new List<GamePiece>();
        
        // Проходим по всему полю
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (gamePieces[x, y] != null && gamePieces[x, y].type == pieceType)
                {
                    piecesOfType.Add(gamePieces[x, y]);
                }
            }
        }
        
        return piecesOfType;
    }
    
    // Метод для создания бомбы на указанной позиции
    void CreateBomb(int x, int y, int pieceType, GamePiece.BombType bombType)
    {
        // Если на этой позиции уже есть фигура, удаляем её
        if (gamePieces[x, y] != null)
        {
            Destroy(gamePieces[x, y].gameObject);
        }
        
        // Создаем новую фигуру того же типа
        GameObject bombObject = Instantiate(gamePiecePrefabs[pieceType], new Vector3(x, y, 0), Quaternion.identity);
        bombObject.transform.parent = transform;
        
        GamePiece bombPiece = bombObject.GetComponent<GamePiece>();
        if (bombPiece != null)
        {
            bombPiece.Init(x, y);
            bombPiece.type = pieceType;
            bombPiece.SetBomb(bombType);
            
            gamePieces[x, y] = bombPiece;
            bombObject.name = "BombPiece (" + x + "," + y + ")";
        }
    }
    
    IEnumerator ClearMatchesAnimation(List<GamePiece> matches)
    {
        // Разделяем обычные фигуры и бомбы
        List<GamePiece> regularPieces = new List<GamePiece>();
        List<GamePiece> bombPieces = new List<GamePiece>();
        
        foreach (GamePiece piece in matches)
        {
            if (piece.bombType != GamePiece.BombType.None)
                bombPieces.Add(piece);
            else
                regularPieces.Add(piece);
        }
        
        // Анимируем взрыв бомб
        foreach (GamePiece bombPiece in bombPieces)
        {
            StartCoroutine(AnimateBombExplosion(bombPiece));
        }
        
        // Ждем чуть дольше для бомб
        if (bombPieces.Count > 0)
            yield return new WaitForSeconds(0.4f);
        
        // Затем анимируем обычные фигуры
        foreach (GamePiece piece in regularPieces)
        {
            StartCoroutine(AnimatePieceDestruction(piece));
        }
        
        // Ждем завершения анимации
        yield return new WaitForSeconds(0.3f);
        
        // Удаляем фигуры из массива и уничтожаем их
        foreach (GamePiece piece in matches)
        {
            int x = piece.xIndex;
            int y = piece.yIndex;
            
            gamePieces[x, y] = null;
            Destroy(piece.gameObject);
        }
        
        // Запускаем процесс падения и заполнения
        StartCoroutine(CollapseAndFillBoard());
    }
    
    // Метод для анимации взрыва бомбы
 IEnumerator AnimateBombExplosion(GamePiece bombPiece)
{
    float duration = 0.4f;
    float elapsedTime = 0;
    Vector3 startScale = bombPiece.transform.localScale;
    
    // Получаем цвет фигуры для эффекта
    Color pieceColor = Color.white;
    SpriteRenderer renderer = bombPiece.GetComponent<SpriteRenderer>();
    if (renderer != null)
    {
        pieceColor = renderer.color;
    }
    
    // Определяем, какие фигуры будут взорваны
    bool isHorizontal = bombPiece.bombType == GamePiece.BombType.Horizontal;
    List<GamePiece> piecesToDestroy = FindPiecesToBomb(bombPiece, bombPiece.bombType);
    
    // Создаем эффект молнии ТОЛЬКО для горизонтальных и вертикальных бомб
    if (bombPiece.bombType == GamePiece.BombType.Horizontal || bombPiece.bombType == GamePiece.BombType.Vertical)
    {
        // Определяем цвет для молнии
        Color lightningColor = isHorizontal ? 
            new Color(1f, 0.5f, 0.1f) : // Оранжевый для горизонтальной
            new Color(0.1f, 0.5f, 1f);  // Голубой для вертикальной
        
        // Создаем эффект молнии, передавая список фигур
        LightningEffect.CreateLightningEffect(
            isHorizontal,
            piecesToDestroy,
            lightningColor
        );
        }  
        // Анимируем исчезновение самой бомбы
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration; // Определение переменной t внутри цикла
            
            // Увеличиваем и уменьшаем размер бомбы
            float scaleFactor = 1 + 0.5f * Mathf.Sin(t * Mathf.PI);
            bombPiece.transform.localScale = startScale * scaleFactor * (1 - t);
            
            // Вращаем для эффекта
            bombPiece.transform.Rotate(0, 0, 720 * Time.deltaTime);
            
            // Делаем прозрачным
            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = 1 - t;
                renderer.color = color;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator AnimatePieceDestruction(GamePiece piece)
    {
        float duration = 0.3f;
        float elapsedTime = 0;
        Vector3 startScale = piece.transform.localScale;
    
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
        
            // Увеличиваем размер и затем уменьшаем до нуля
            float scaleFactor = 1 + 0.3f * Mathf.Sin(t * Mathf.PI);
            piece.transform.localScale = startScale * scaleFactor * (1 - t);
        
            // Вращаем для эффекта
            piece.transform.Rotate(0, 0, 720 * Time.deltaTime); // 720 градусов в секунду
        
            // Постепенно делаем прозрачным (требуется Material с поддержкой прозрачности)
            SpriteRenderer renderer = piece.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = 1 - t;
                renderer.color = color;
            }
        
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }    
    
    IEnumerator CollapseAndFillBoard()
    {
        isFilling = true;
        
        // Подождем немного перед падением
        yield return new WaitForSeconds(0.2f);
        
        // Сначала падение существующих фигур
        MoveDownAllPieces();
        
        // Ждем завершения анимации падения
        yield return new WaitForSeconds(fallTime);
        
        // Создаем новые фигуры на пустых местах
        FillEmptySpaces();
        
        // Ждем завершения анимации заполнения
        yield return new WaitForSeconds(fillTime);
        
        // Проверяем, есть ли новые совпадения
        List<GamePiece> newMatches = FindMatches();
        if (newMatches.Count > 0)
        {
            // Если есть, удаляем их и запускаем процесс снова
            ClearMatches(newMatches);
        }
        else
        {
            // Если нет, разрешаем новые ходы
            isSwapping = false;
            isFilling = false;
            selectedPiece = null;
        }
    }
    
    void MoveDownAllPieces()
    {
        // Проходим сверху вниз, начиная со второго ряда
        for (int x = 0; x < width; x++)
        {
            for (int y = 1; y < height; y++)
            {
                if (gamePieces[x, y] != null)
                {
                    // Для каждой фигуры проверяем, сколько пустых мест под ней
                    int emptySpaces = CountEmptySpacesBelow(x, y);
                    
                    if (emptySpaces > 0)
                    {
                        // Двигаем фигуру вниз на количество пустых мест
                        GamePiece piece = gamePieces[x, y];
                        int targetY = y - emptySpaces;
                        
                        // Перемещаем в массиве
                        gamePieces[x, y] = null;
                        gamePieces[x, targetY] = piece;
                        
                        // Обновляем индексы
                        piece.Init(x, targetY);
                        
                        // Анимируем перемещение
                        StartCoroutine(MovePiece(piece, new Vector2(x, targetY)));
                    }
                }
            }
        }
    }
    
    int CountEmptySpacesBelow(int x, int y)
    {
        int count = 0;
        
        // Проверяем все позиции ниже текущей
        for (int i = y - 1; i >= 0; i--)
        {
            if (gamePieces[x, i] == null)
            {
                count++;
            }
        }
        
        return count;
    }
    
    void FillEmptySpaces()
    {
        // Для каждой колонки
        for (int x = 0; x < width; x++)
        {
            // Считаем, сколько пустых мест в колонке
            int emptyCount = 0;
            
            for (int y = 0; y < height; y++)
            {
                if (gamePieces[x, y] == null)
                {
                    emptyCount++;
                }
            }
            
            // Если есть пустые места, создаем новые фигуры сверху
            if (emptyCount > 0)
            {
                for (int i = 0; i < emptyCount; i++)
                {
                    // Создаем фигуру над доской и анимируем её падение
                    int randomIndex = Random.Range(0, gamePiecePrefabs.Length);
                    Vector2 spawnPosition = new Vector2(x, height + i);
                    
                    GameObject piece = Instantiate(gamePiecePrefabs[randomIndex], spawnPosition, Quaternion.identity);
                    piece.transform.parent = transform;
                    
                    GamePiece gamePiece = piece.GetComponent<GamePiece>();
                    
                    // Ищем первую пустую позицию снизу в этой колонке
                    int targetY = 0;
                    for (int y = 0; y < height; y++)
                    {
                        if (gamePieces[x, y] == null)
                        {
                            targetY = y;
                            break;
                        }
                    }
                    
                    // Устанавливаем фигуру в массив
                    gamePieces[x, targetY] = gamePiece;
                    gamePiece.Init(x, targetY);
                    gamePiece.type = randomIndex;
                    piece.name = "Piece (" + x + "," + targetY + ")";
                    
                    // Анимируем падение
                    StartCoroutine(MovePiece(gamePiece, new Vector2(x, targetY)));
                }
            }
        }
    }
    
    IEnumerator MovePiece(GamePiece piece, Vector2 targetPosition)
    {
        Vector2 startPosition = piece.transform.position;
        float elapsedTime = 0;
        
        // Используем fixedScale из GamePiece
        Vector3 originalScale = piece.fixedScale;
        
        while (elapsedTime < fallTime)
        {
            float t = elapsedTime / fallTime;
            
            // Используем эффект отскока для более естественного падения
            float finalT = t < 0.7f ? EasingFunctions.EaseInQuad(t / 0.7f) : EasingFunctions.EaseOutBounce((t - 0.7f) / 0.3f);
            
            piece.transform.position = Vector2.Lerp(startPosition, targetPosition, finalT);
            
            // Слегка сжимаем при падении и растягиваем при отскоке, но более умеренно
            float scaleY = 1f;
            float scaleX = 1f;
            
            if (t > 0.7f && t < 0.9f) {
                // Сжатие при подготовке к "приземлению"
                float squashT = (t - 0.7f) / 0.2f;
                scaleY = 1f - 0.1f * squashT;
                scaleX = 1f + 0.05f * squashT;
            } 
            else if (t >= 0.9f) {
                // Возврат к нормальному размеру
                float returnT = (t - 0.9f) / 0.1f;
                scaleY = 0.9f + 0.1f * returnT;
                scaleX = 1.05f - 0.05f * returnT;
            }
            
            piece.transform.localScale = new Vector3(
                originalScale.x * scaleX, 
                originalScale.y * scaleY, 
                originalScale.z);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Гарантированно устанавливаем правильный масштаб
        piece.transform.position = targetPosition;
        piece.transform.localScale = originalScale;
            }
    
    // Метод для поиска возможного хода
    private bool FindPossibleMatch(out GamePiece piece1, out GamePiece piece2)
    {
        piece1 = null;
        piece2 = null;
        
        // Проверяем все возможные ходы
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GamePiece currentPiece = gamePieces[x, y];
                if (currentPiece == null) continue;
                
                // Проверяем ход вправо
                if (x < width - 1)
                {
                    GamePiece rightPiece = gamePieces[x + 1, y];
                    if (rightPiece != null)
                    {
                        // Временно меняем фигуры
                        gamePieces[x, y] = rightPiece;
                        gamePieces[x + 1, y] = currentPiece;
                        
                        // Проверяем, создает ли это совпадение
                        List<GamePiece> matches = FindMatches();
                        
                        // Возвращаем обратно
                        gamePieces[x, y] = currentPiece;
                        gamePieces[x + 1, y] = rightPiece;
                        
                        if (matches.Count > 0)
                        {
                            piece1 = currentPiece;
                            piece2 = rightPiece;
                            return true;
                        }
                    }
                }
                
                // Проверяем ход вверх
                if (y < height - 1)
                {
                    GamePiece upPiece = gamePieces[x, y + 1];
                    if (upPiece != null)
                    {
                        // Временно меняем фигуры
                        gamePieces[x, y] = upPiece;
                        gamePieces[x, y + 1] = currentPiece;
                        
                        // Проверяем, создает ли это совпадение
                        List<GamePiece> matches = FindMatches();
                        
                        // Возвращаем обратно
                        gamePieces[x, y] = currentPiece;
                        gamePieces[x, y + 1] = upPiece;
                        
                        if (matches.Count > 0)
                        {
                            piece1 = currentPiece;
                            piece2 = upPiece;
                            return true;
                        }
                    }
                }
            }
        }
        
        return false;
    }
    
    // Показывает подсказку игроку
    private void ShowHint()
    {
        // Находим возможный ход
        GamePiece piece1, piece2;
        if (FindPossibleMatch(out piece1, out piece2))
        {
            showingHint = true;
            hintPieces[0] = piece1;
            hintPieces[1] = piece2;
            
            // Анимируем подсказку
            StartCoroutine(AnimateHint());
        }
        else
        {
            // Если ходов не найдено, можно перемешать доску
            Debug.Log("Ходов не найдено!");
        }
    }
    
    // Прекращает показ подсказки
    private void StopHint()
    {
        if (showingHint)
        {
            showingHint = false;
            StopCoroutine("AnimateHint");
            
            // Возвращаем обычный размер
            if (hintPieces[0] != null)
                hintPieces[0].transform.localScale = hintPieces[0].fixedScale;
            
            if (hintPieces[1] != null)
                hintPieces[1].transform.localScale = hintPieces[1].fixedScale;
        }
    }
    
    // Анимация подсказки
    private IEnumerator AnimateHint()
    {
        float scaleAmount = 0.1f;
        float scaleSpeed = 2f;
        
        while (showingHint)
        {
            // Пульсирующая анимация
            float scale = 1 + scaleAmount * Mathf.Sin(Time.time * scaleSpeed);
            
            // Применяем масштаб к фигурам подсказки
            if (hintPieces[0] != null)
                hintPieces[0].transform.localScale = hintPieces[0].fixedScale * scale;
            
            if (hintPieces[1] != null)
                hintPieces[1].transform.localScale = hintPieces[1].fixedScale * scale;
            
            yield return null;
        }
    }
    
    // Метод перенесён внутрь класса Board (был вне класса)
    IEnumerator AnimateColorBombEffect(GamePiece bombPiece, List<GamePiece> targetPieces)
    {
        // Получаем цвет бомбы
        Color bombColor = Color.magenta;
        SpriteRenderer renderer = bombPiece.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            bombColor = renderer.color;
        }
        
        // Анимация активации бомбы
        float activationDuration = 0.3f;
        float elapsedTime = 0;
        Vector3 originalScale = bombPiece.transform.localScale;
        
        while (elapsedTime < activationDuration)
        {
            float t = elapsedTime / activationDuration;
            float scale = 1f + 0.5f * (1f - t);
            bombPiece.transform.localScale = originalScale * scale;
            bombPiece.transform.Rotate(0, 0, 360 * Time.deltaTime);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Создаем частицы, летящие к целям
        foreach (GamePiece targetPiece in targetPieces)
        {
            if (targetPiece != bombPiece)
            {
                ColorBombEffect.CreateEffect(
                    bombPiece.transform.position, 
                    targetPiece.transform.position, 
                    bombColor
                );
                
                // Делаем небольшую паузу между запуском частиц для лучшего визуального эффекта
                yield return new WaitForSeconds(0.02f);
            }
        }
        
        // Ждем, пока частицы долетят до целей
        yield return new WaitForSeconds(0.5f);
        
        // Удаляем фигуры
        ClearMatches(targetPieces);
    }

    // Метод перенесён внутрь класса Board (был вне класса)
    void CreateColorBombParticles(Vector3 startPosition, Vector3 targetPosition, Color particleColor)
    {
        GameObject particleObj = new GameObject("ColorBombParticles");
        particleObj.transform.position = startPosition;
        
        // Создаем систему частиц
        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
        
        // Настраиваем основные параметры
        var main = ps.main;
        main.startLifetime = 0.5f;
        main.startSpeed = Vector3.Distance(startPosition, targetPosition) * 2f; // Скорость зависит от расстояния
        main.startSize = 0.2f;
        main.startColor = particleColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        // Уничтожаем после проигрывания
        main.stopAction = ParticleSystemStopAction.Destroy;
        
        // Настраиваем эмиссию
        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[] { 
            new ParticleSystem.Burst(0.0f, 10) // Выпускаем 10 частиц сразу
        });
        emission.rateOverTime = 0; // Не выпускаем частицы постоянно
        
        // Настраиваем форму эмиссии
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;
        
        // Направляем частицы к цели
        Vector3 direction = (targetPosition - startPosition).normalized;
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = direction.x * 5;
        velocity.y = direction.y * 5;
        velocity.z = direction.z * 5;
        
        // Добавляем модуль цвета по времени жизни
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(particleColor, 0.0f),
                new GradientColorKey(Color.white, 1.0f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        
        colorOverLifetime.color = gradient;
        
        // Добавляем размер по времени жизни
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 0.5f);
        
        // Запускаем систему частиц
        ps.Play();
    }
}








