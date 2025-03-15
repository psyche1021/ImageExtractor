using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class ImageLoader : MonoBehaviour
{
    public InputField inputField; // 배율 입력할 인풋필드
    public Text multipleText; // 입력한 배율의 수치를 보여줄 Text
    public float multipleValue; // 입력한 배율의 값을 저장할 변수
    private string resultText = "Result.txt"; // 텍스트 파일 형식
    public string folderPath; // 폴더 경로
    public Texture2D[] imageArray = new Texture2D[32]; // 불러올 이미지 배열
    public Image[] gridArray = new Image[32]; // 만들어둔 그리드 배열
    public Image[] diffArray = new Image[16]; // 만들어둔 차이 배열
    public Image[] multiArray = new Image[16]; // 만들어둔 차이 배수 배열
    private string averageFilePath; // 평균값 파일 경로

    async void Start()
    {
        // 바탕화면 경로 탐색 및 바탕화면의 Image 폴더를 이미지 저장 경로로 지정
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        folderPath = Path.Combine(desktopPath, "Image");
        averageFilePath = Path.Combine(folderPath, resultText);

        multipleValue = 1.0f; // 인풋필드 기본값
        inputField.ActivateInputField(); // 인풋필드 마우스 안눌러도 활성화되게

        // 폴더 존재 여부 확인
        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"Image 폴더가 존재하지 않습니다: {folderPath}");
            Directory.CreateDirectory(folderPath); // Image 폴더 생성

            // 사용자 알림
            Debug.LogWarning("Image 폴더가 생성되었습니다. 이미지를 해당 폴더에 추가한 후 프로그램을 다시 실행하세요.");

            // 에디터에서 실행 중인 경우 종료
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // 빌드된 실행 파일에서는 프로그램 종료
            Application.Quit();
#endif
            return;
        }

        // 이미지 불러오기
        await LoadImagesAsync();

        // 이미지 RGB 평균 계산 후 그리드에 표시
        await CalculateRGBAsync();

        // 이미지 RGB 값 TXT 파일로 저장
        await SaveRGBToFileAsync();
    }

    void Update()
    {
        // 현재 몇배율인지
        multipleText.text = $"배율 : {multipleValue}";
        
        // onEndEdit 시 이벤트 호출
        inputField.onEndEdit.AddListener(UpdateValue);

        // 배율 반영한 RGB
        MultipleRGB();
    }

    void UpdateValue(string text)
    {
        if (float.TryParse(text, out multipleValue))
        {
            // 현재 배율 디버그 띄워줌
            Debug.Log("배율 업데이트: " + multipleValue);
        }
    }

    async Task LoadImagesAsync()
    {
        for (int i = 0; i < imageArray.Length; i++)
        {
            // jpg 파일 경로 설정
            string filePath = Path.Combine(folderPath, $"{i + 1}.jpg");

            // jpg 파일이 없을 경우 png 파일로 시도
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(folderPath, $"{i + 1}.png");
            }

            if (File.Exists(filePath))
            {
                // Read/Write 권한 활성화
                File.SetAttributes(filePath, FileAttributes.Normal);

                // 이미지 파일 읽기 (비동기적 파일 읽기)
                byte[] fileData = await File.ReadAllBytesAsync(filePath);
                Texture2D texture = new Texture2D(2, 2);

                // 텍스처에 이미지 데이터 로드
                texture.LoadImage(fileData);

                // 이미지 배열에 저장
                imageArray[i] = texture;
            }
        }
    }

    async Task CalculateRGBAsync()
    {
        for (int i = 0; i < imageArray.Length; i++)
        {
            if (imageArray[i] != null)
            {
                Texture2D texture = imageArray[i];
                Color[] pixels = texture.GetPixels();
                float totalR = 0f, totalG = 0f, totalB = 0f;

                foreach (Color pixel in pixels)
                {
                    totalR += pixel.r;
                    totalG += pixel.g;
                    totalB += pixel.b;
                }

                // RGB 평균 계산
                float avgR = totalR / pixels.Length;
                float avgG = totalG / pixels.Length;
                float avgB = totalB / pixels.Length;

                // 평균값을 소수점 두 자리까지 계산 후 해당 이미지를 그리드에 표시
                gridArray[i].color = new Color(avgR, avgG, avgB);

                // 결과를 텍스트 파일에 출력
                string rgbData = $"{i + 1}, {avgR * 255:F2}, {avgG * 255:F2}, {avgB * 255:F2}";
                await SaveRGBToFileAsync(rgbData);
            }
        }

        // GridArray의 차이값을 DiffArray에 계산하여 저장
        for (int i = 0; i < 16; i++)
        {
            if (gridArray[i] != null && gridArray[i + 16] != null)
            {
                // 첫 번째 이미지 (GridArray[i])와 두 번째 이미지 (GridArray[i+16])의 RGB 차이 계산
                Color gridColor1 = gridArray[i].color;
                Color gridColor2 = gridArray[i + 16].color;

                // RGB 차이 계산
                float diffR = Mathf.Abs(gridColor1.r - gridColor2.r);
                float diffG = Mathf.Abs(gridColor1.g - gridColor2.g);
                float diffB = Mathf.Abs(gridColor1.b - gridColor2.b);

                // 차이값을 DiffArray에 할당
                diffArray[i].color = new Color(diffR, diffG, diffB);

                // DiffArray의 차이값을 메모장에 출력 (33번부터 시작)
                string diffData = $"{i + 33}, {diffR * 255:F2}, {diffG * 255:F2}, {diffB * 255:F2}";
                await SaveRGBToFileAsync(diffData);
            }
        }
    }

    async Task SaveRGBToFileAsync(string rgbData = "")
    {
        if (!string.IsNullOrEmpty(rgbData))
        {
            // 텍스트 파일에 RGB 값 비동기적으로 저장
            await File.AppendAllTextAsync(averageFilePath, rgbData + "\n", Encoding.UTF8);
        }
    }

    void MultipleRGB()
    {
        for (int i = 0; i < 16; i++)
        {
            if (diffArray[i] != null)
            {
                // diffArray에서 계산된 RGB 차이값을 multipleValue 배수만큼 곱한 RGB 값으로 설정
                Color diffColor = diffArray[i].color;

                float multiR = diffColor.r * multipleValue;
                float multiG = diffColor.g * multipleValue;
                float multiB = diffColor.b * multipleValue;

                // 0~255 범위로 클램프
                multiR = Mathf.Clamp(multiR, 0f, 1f);
                multiG = Mathf.Clamp(multiG, 0f, 1f);
                multiB = Mathf.Clamp(multiB, 0f, 1f);

                // multiArray[i]에 배수 적용한 RGB 값을 반영 (Color는 0~1 범위로 설정)
                multiArray[i].color = new Color(multiR, multiG, multiB);
            }
        }
    }

    void OnApplicationQuit()
    {
        // 프로그램 종료 시 평균.txt 파일 삭제
        if (File.Exists(averageFilePath))
        {
            File.Delete(averageFilePath);
            Debug.Log($"{resultText} 파일이 삭제되었습니다.");
        }
    }
}
