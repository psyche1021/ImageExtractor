using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class ImageLoader : MonoBehaviour
{
    public InputField inputField; // ���� �Է��� ��ǲ�ʵ�
    public Text multipleText; // �Է��� ������ ��ġ�� ������ Text
    public float multipleValue; // �Է��� ������ ���� ������ ����
    private string resultText = "Result.txt"; // �ؽ�Ʈ ���� ����
    public string folderPath; // ���� ���
    public Texture2D[] imageArray = new Texture2D[32]; // �ҷ��� �̹��� �迭
    public Image[] gridArray = new Image[32]; // ������ �׸��� �迭
    public Image[] diffArray = new Image[16]; // ������ ���� �迭
    public Image[] multiArray = new Image[16]; // ������ ���� ��� �迭
    private string averageFilePath; // ��հ� ���� ���

    async void Start()
    {
        // ����ȭ�� ��� Ž�� �� ����ȭ���� Image ������ �̹��� ���� ��η� ����
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        folderPath = Path.Combine(desktopPath, "Image");
        averageFilePath = Path.Combine(folderPath, resultText);

        multipleValue = 1.0f; // ��ǲ�ʵ� �⺻��
        inputField.ActivateInputField(); // ��ǲ�ʵ� ���콺 �ȴ����� Ȱ��ȭ�ǰ�

        // ���� ���� ���� Ȯ��
        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"Image ������ �������� �ʽ��ϴ�: {folderPath}");
            Directory.CreateDirectory(folderPath); // Image ���� ����

            // ����� �˸�
            Debug.LogWarning("Image ������ �����Ǿ����ϴ�. �̹����� �ش� ������ �߰��� �� ���α׷��� �ٽ� �����ϼ���.");

            // �����Ϳ��� ���� ���� ��� ����
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // ����� ���� ���Ͽ����� ���α׷� ����
            Application.Quit();
#endif
            return;
        }

        // �̹��� �ҷ�����
        await LoadImagesAsync();

        // �̹��� RGB ��� ��� �� �׸��忡 ǥ��
        await CalculateRGBAsync();

        // �̹��� RGB �� TXT ���Ϸ� ����
        await SaveRGBToFileAsync();
    }

    void Update()
    {
        // ���� ���������
        multipleText.text = $"���� : {multipleValue}";
        
        // onEndEdit �� �̺�Ʈ ȣ��
        inputField.onEndEdit.AddListener(UpdateValue);

        // ���� �ݿ��� RGB
        MultipleRGB();
    }

    void UpdateValue(string text)
    {
        if (float.TryParse(text, out multipleValue))
        {
            // ���� ���� ����� �����
            Debug.Log("���� ������Ʈ: " + multipleValue);
        }
    }

    async Task LoadImagesAsync()
    {
        for (int i = 0; i < imageArray.Length; i++)
        {
            // jpg ���� ��� ����
            string filePath = Path.Combine(folderPath, $"{i + 1}.jpg");

            // jpg ������ ���� ��� png ���Ϸ� �õ�
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(folderPath, $"{i + 1}.png");
            }

            if (File.Exists(filePath))
            {
                // Read/Write ���� Ȱ��ȭ
                File.SetAttributes(filePath, FileAttributes.Normal);

                // �̹��� ���� �б� (�񵿱��� ���� �б�)
                byte[] fileData = await File.ReadAllBytesAsync(filePath);
                Texture2D texture = new Texture2D(2, 2);

                // �ؽ�ó�� �̹��� ������ �ε�
                texture.LoadImage(fileData);

                // �̹��� �迭�� ����
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

                // RGB ��� ���
                float avgR = totalR / pixels.Length;
                float avgG = totalG / pixels.Length;
                float avgB = totalB / pixels.Length;

                // ��հ��� �Ҽ��� �� �ڸ����� ��� �� �ش� �̹����� �׸��忡 ǥ��
                gridArray[i].color = new Color(avgR, avgG, avgB);

                // ����� �ؽ�Ʈ ���Ͽ� ���
                string rgbData = $"{i + 1}, {avgR * 255:F2}, {avgG * 255:F2}, {avgB * 255:F2}";
                await SaveRGBToFileAsync(rgbData);
            }
        }

        // GridArray�� ���̰��� DiffArray�� ����Ͽ� ����
        for (int i = 0; i < 16; i++)
        {
            if (gridArray[i] != null && gridArray[i + 16] != null)
            {
                // ù ��° �̹��� (GridArray[i])�� �� ��° �̹��� (GridArray[i+16])�� RGB ���� ���
                Color gridColor1 = gridArray[i].color;
                Color gridColor2 = gridArray[i + 16].color;

                // RGB ���� ���
                float diffR = Mathf.Abs(gridColor1.r - gridColor2.r);
                float diffG = Mathf.Abs(gridColor1.g - gridColor2.g);
                float diffB = Mathf.Abs(gridColor1.b - gridColor2.b);

                // ���̰��� DiffArray�� �Ҵ�
                diffArray[i].color = new Color(diffR, diffG, diffB);

                // DiffArray�� ���̰��� �޸��忡 ��� (33������ ����)
                string diffData = $"{i + 33}, {diffR * 255:F2}, {diffG * 255:F2}, {diffB * 255:F2}";
                await SaveRGBToFileAsync(diffData);
            }
        }
    }

    async Task SaveRGBToFileAsync(string rgbData = "")
    {
        if (!string.IsNullOrEmpty(rgbData))
        {
            // �ؽ�Ʈ ���Ͽ� RGB �� �񵿱������� ����
            await File.AppendAllTextAsync(averageFilePath, rgbData + "\n", Encoding.UTF8);
        }
    }

    void MultipleRGB()
    {
        for (int i = 0; i < 16; i++)
        {
            if (diffArray[i] != null)
            {
                // diffArray���� ���� RGB ���̰��� multipleValue �����ŭ ���� RGB ������ ����
                Color diffColor = diffArray[i].color;

                float multiR = diffColor.r * multipleValue;
                float multiG = diffColor.g * multipleValue;
                float multiB = diffColor.b * multipleValue;

                // 0~255 ������ Ŭ����
                multiR = Mathf.Clamp(multiR, 0f, 1f);
                multiG = Mathf.Clamp(multiG, 0f, 1f);
                multiB = Mathf.Clamp(multiB, 0f, 1f);

                // multiArray[i]�� ��� ������ RGB ���� �ݿ� (Color�� 0~1 ������ ����)
                multiArray[i].color = new Color(multiR, multiG, multiB);
            }
        }
    }

    void OnApplicationQuit()
    {
        // ���α׷� ���� �� ���.txt ���� ����
        if (File.Exists(averageFilePath))
        {
            File.Delete(averageFilePath);
            Debug.Log($"{resultText} ������ �����Ǿ����ϴ�.");
        }
    }
}
