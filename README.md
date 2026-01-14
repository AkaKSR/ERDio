# ERDio - Entity Relationship Diagram Editor

ERDio는 WPF 기반의 ERD(Entity Relationship Diagram) 편집기입니다. 데이터베이스 설계를 위한 직관적인 시각적 도구를 제공합니다.

![.NET](https://img.shields.io/badge/.NET-10.0-blue)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)
![License](https://img.shields.io/badge/License-MIT-green)

## ✨ 주요 기능

### 테이블 관리
- 📝 새 테이블 추가
- 테이블 이름 더블클릭으로 편집
- 테이블 색상 변경 (우클릭 메뉴)
- 테이블 삭제
- 드래그로 테이블 위치 이동

### 컬럼 관리
- ➕ 컬럼 추가 (헤더의 + 버튼 또는 우클릭 메뉴)
- 컬럼 이름/데이터타입 더블클릭으로 편집
- Primary Key (🔑) / Foreign Key (🔗) 토글
- Nullable 속성 토글
- 컬럼 순서 이동 (위/아래)
- 컬럼 삭제

### 관계선 관리
- 🔗 테이블 간 관계 추가
- 관계 타입 지원:
  - One to One (1:1)
  - One to Many (1:N)
  - Many to One (N:1)
  - Many to Many (N:M)
- Crow's Foot 표기법으로 관계 표시
- 베지어 커브 렌더링
- 관계선 우클릭으로 삭제

### 캔버스 기능
- 마우스 휠로 확대/축소 (25% ~ 200%)
- 배경 드래그로 캔버스 이동
- 미니맵으로 전체 뷰 확인

### 파일 관리
- 💾 ERD 저장 (.erd, .json 형식)
- 📂 ERD 불러오기
- 📷 이미지 내보내기 (PNG, JPG)

## 🚀 시작하기

### 요구 사항
- .NET 10.0 SDK
- Windows 운영 체제

### 빌드 및 실행

```bash
# 프로젝트 클론
git clone https://github.com/your-username/ERDio.git
cd ERDio

# Debug 빌드
dotnet build

# Release 빌드
dotnet build -c Release

# 실행
dotnet run
```

### 출력 파일 위치
- Debug: `bin/Debug/net10.0-windows/ERDio.exe`
- Release: `bin/Release/net10.0-windows/ERDio.exe`

## 📖 사용법

### 테이블 추가
1. 툴바의 📝 버튼 클릭
2. 새 테이블이 캔버스에 추가됨
3. 테이블 이름을 더블클릭하여 편집

### 컬럼 편집
1. 컬럼 이름 또는 데이터타입을 더블클릭하여 편집
2. Enter 또는 클릭으로 편집 완료
3. 우클릭 메뉴로 PK/FK/Nullable 설정

### 관계 추가
1. 툴바의 🔗 버튼 클릭
2. Source/Target 테이블 및 컬럼 선택
3. 관계 타입 선택 후 Add 클릭

### 저장/불러오기
- 💾 버튼: ERD를 .erd 또는 .json 파일로 저장
- 📂 버튼: 저장된 ERD 파일 불러오기

### 이미지 내보내기
- 📷 버튼 클릭하여 PNG 또는 JPG로 내보내기

## 🏗️ 프로젝트 구조

```
ERDio/
├── App.xaml                 # 애플리케이션 진입점
├── MainWindow.xaml          # 메인 윈도우 UI
├── MainWindow.xaml.cs       # 메인 윈도우 로직
├── RelationshipDialog.xaml  # 관계 추가 다이얼로그
├── Controls/
│   ├── TableControl.xaml    # 테이블 컨트롤 UI
│   ├── TableControl.xaml.cs # 테이블 컨트롤 로직
│   └── RelationshipLine.cs  # 관계선 렌더링
├── Models/
│   ├── Table.cs             # 테이블 모델
│   ├── Column.cs            # 컬럼 모델
│   └── Relationship.cs      # 관계 모델
└── ViewModels/
    └── MainViewModel.cs     # 메인 뷰모델
```

## 🎨 스크린샷

### 메인 화면
- 다크 테마 기반 UI
- 그리드 패턴 배경
- 우측 상단 미니맵

### 지원되는 데이터타입
- VARCHAR2, NUMBER, DATE 등 Oracle 호환 타입
- 사용자 정의 데이터타입 입력 가능

## 📝 라이선스

MIT License

## 🤝 기여하기

1. 이 저장소를 Fork 합니다
2. Feature 브랜치를 생성합니다 (`git checkout -b feature/AmazingFeature`)
3. 변경사항을 커밋합니다 (`git commit -m 'Add some AmazingFeature'`)
4. 브랜치에 Push 합니다 (`git push origin feature/AmazingFeature`)
5. Pull Request를 생성합니다
