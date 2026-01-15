# ERDio - Entity Relationship Diagram Editor

ERDio는 WPF 기반의 ERD(Entity Relationship Diagram) 편집기입니다. 데이터베이스 설계를 위한 직관적인 시각적 도구를 제공합니다.

![.NET](https://img.shields.io/badge/.NET-10.0-blue)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)
![License](https://img.shields.io/badge/License-MIT-green)

## ✨ 주요 기능

### 테이블 관리
- 📝 새 테이블 추가
- 테이블 이름 더블클릭으로 편집
- 테이블 색상 변경 (우클릭 메뉴: Red, Blue, Green, Purple, Orange, Teal)
- 테이블 삭제
- 드래그로 테이블 위치 이동

### 컬럼 관리
- ➕ 컬럼 추가 (헤더의 + 버튼 또는 우클릭 메뉴)
- 컬럼 이름/데이터타입 더블클릭으로 편집
- **Default 값 더블클릭으로 편집**
- **Comment 더블클릭으로 편집**
- Primary Key (🔑) / Foreign Key (🔗) 토글
- Nullable 속성 토글 (NULL / N-N)
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

### SQL 내보내기
- 🗃️ SQL DDL 문 생성 및 내보내기
- 다중 데이터베이스 지원:
  - **MySQL**
  - **PostgreSQL**
  - **Oracle**
  - **Tibero**
- 자동 데이터타입 변환
- PRIMARY KEY 제약조건 생성
- FOREIGN KEY 제약조건 생성
- 테이블/컬럼 COMMENT 지원
- 클립보드 복사 기능

## 🚀 시작하기

### 요구 사항
- .NET 10.0 SDK
- Windows 운영 체제

### 빌드 및 실행

```bash
# 프로젝트 클론
git clone https://github.com/AkaKSR/ERDio.git
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
2. Default 값 또는 Comment를 더블클릭하여 편집
3. Enter 또는 다른 곳 클릭으로 편집 완료
4. Escape 키로 편집 취소
5. 우클릭 메뉴로 PK/FK/Nullable 설정

### 관계 추가
1. 툴바의 🔗 버튼 클릭
2. Source/Target 테이블 및 컬럼 선택
3. 관계 타입 선택 후 Add 클릭

### 저장/불러오기
- 💾 버튼: ERD를 .erd 또는 .json 파일로 저장
- 📂 버튼: 저장된 ERD 파일 불러오기

### 이미지 내보내기
- 📷 버튼 클릭하여 PNG 또는 JPG로 내보내기

### SQL 내보내기
1. 툴바의 🗃️ 버튼 클릭
2. 대상 데이터베이스 선택 (MySQL, PostgreSQL, Oracle, Tibero)
3. 생성된 SQL 확인
4. Copy 버튼으로 클립보드에 복사

## 🏗️ 프로젝트 구조

```
ERDio/
├── App.xaml                  # 애플리케이션 진입점
├── App.xaml.cs
├── MainWindow.xaml           # 메인 윈도우 UI
├── MainWindow.xaml.cs        # 메인 윈도우 로직
├── RelationshipDialog.xaml   # 관계 추가 다이얼로그
├── RelationshipDialog.xaml.cs
├── SqlExportDialog.xaml      # SQL 내보내기 다이얼로그
├── SqlExportDialog.xaml.cs   # SQL 생성 및 데이터타입 변환 로직
├── Controls/
│   ├── TableControl.xaml     # 테이블 컨트롤 UI
│   ├── TableControl.xaml.cs  # 테이블 컨트롤 로직 (인라인 편집)
│   └── RelationshipLine.cs   # 관계선 렌더링 (Crow's Foot)
├── Models/
│   ├── Table.cs              # 테이블 모델
│   ├── Column.cs             # 컬럼 모델 (Name, DataType, PK, FK, Nullable, Default, Comment)
│   └── Relationship.cs       # 관계 모델 (RelationType enum 포함)
└── ViewModels/
    └── MainViewModel.cs      # 메인 뷰모델 (샘플 데이터 포함)
```

## 📊 샘플 데이터

애플리케이션 실행 시 쇼핑몰 ERD 샘플이 자동으로 로드됩니다:

| 테이블 | 설명 | 주요 컬럼 |
|--------|------|-----------|
| **MEMBER** | 회원 정보 | MEMBER_ID (PK), NAME, EMAIL, PHONE |
| **CATEGORY** | 상품 카테고리 (자기참조) | CATEGORY_ID (PK), PARENT_ID (FK) |
| **PRODUCT** | 상품 정보 | PRODUCT_ID (PK), CATEGORY_ID (FK) |
| **ORDERS** | 주문 정보 | ORDER_ID (PK), MEMBER_ID (FK) |
| **ORDER_DETAIL** | 주문 상세 (다대다 해소) | DETAIL_ID (PK), ORDER_ID (FK), PRODUCT_ID (FK) |

## 🎨 UI 특징

- **다크 테마** 기반 UI
- **그리드 패턴** 배경
- 우측 상단 **미니맵**
- 색상 코드로 테이블 구분
- Crow's Foot 표기법 관계선

## 🔧 지원 데이터타입

### Oracle/Tibero 형식 (기본)
- `VARCHAR2(n)`, `CHAR(n)`
- `NUMBER`, `NUMBER(n)`, `NUMBER(p,s)`
- `DATE`, `CLOB`, `BLOB`

### 자동 변환
| Oracle | MySQL | PostgreSQL |
|--------|-------|------------|
| VARCHAR2 | VARCHAR | VARCHAR |
| NUMBER | INT/DECIMAL | INTEGER/NUMERIC |
| DATE | DATETIME | TIMESTAMP |
| CLOB | TEXT | TEXT |
| BLOB | BLOB | BYTEA |

## 📝 라이선스

MIT License

## 🤝 기여하기

1. 이 저장소를 Fork 합니다
2. Feature 브랜치를 생성합니다 (`git checkout -b feature/AmazingFeature`)
3. 변경사항을 커밋합니다 (`git commit -m 'Add some AmazingFeature'`)
4. 브랜치에 Push 합니다 (`git push origin feature/AmazingFeature`)
5. Pull Request를 생성합니다

---

Made with ❤️ using WPF and .NET 10.0
