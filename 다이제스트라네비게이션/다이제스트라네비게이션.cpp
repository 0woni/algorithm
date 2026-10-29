#include <iostream>
#include <vector>
#include <cmath>

using namespace std;

const int N = 21, INF = 99999999;

// 거리 (가중치 역할)
int dist(pair<int, int>a, pair<int, int> b) { // 두 객체를 하나로 취급 할 수 있게 pair 클래스 사용
	int x = a.first - b.first; // 각각 a와 b의 첫 번째 요소 (x 좌표)
	int y = a.second - b.second; // 두 번째 요소 (y 좌표)
	return (int)sqrt(x * x + y * y); // 유클리드 거리 공식
}
// n : 건물(노드) 개수, w : 건물 간 이동 거리(인접 행렬), length : 출발 건물부터 각 건물까지의 최단 거리 저장, touch : 해당 건물로 오기 직전의 건물을 저장(경로상 바로 앞의 부모 정점 인덱스)
void dijkstra(int n, int w[][N + 1], int start, int length[], int touch[]) {
	bool isvisit[N + 1] = { false }; // 검토 여부 ( 검토한 노드에 대해서 재검토 X ), 수도 코드에서 length[vnear] = -1가 수행하던 노드 순환 방지에 대한 역할

	// 초기화
	for (int i = 1; i <= n; i++) {
		length[i] = w[start][i];
		touch[i] = start;
		isvisit[i] = false;
	}

	length[start] = 0; 
	isvisit[start] = true; // 출발지점 방문 마킹

	for (int a = 1; a <= n - 1; a++) 
	{
		int min = INF, vnear = -1;
		// 가까운 미방문 노드 
		for (int i = 1; i <= n; i++) {
			if (!isvisit[i] && length[i] < min) {
				min = length[i];
				vnear = i;
			}
		}
		// 선택된 노드 방문 처리
		if (vnear == -1) break;
		isvisit[vnear] = true;

		for (int i = 1; i <= n; i++) { // vnear를 거쳐서 가는 경로가 기존 거리보다 짧다면 갱신
			if (!isvisit[i] && length[vnear] + w[vnear][i] < length[i]) {
				length[i] = length[vnear] + w[vnear][i];
				touch[i] = vnear; // i번 건물로 오기 직전의 건물을 기록
			}
		}
	}
}

int main() {
	pair<int, int> p[N+1] = {
		{0,0}, // 인덱스 1번부터 시작
		{50,250},  // 01 평생교육관
		{100,250},  // 02 예술관 
		{100,200},  // 03 국제 교육관
		{200,150},  // 04 인문관
		{270,160},  // 05 사범관
		{325,180},  // 06 상경관
		{300,250},  // 07 중앙도서관
		{450,200},  // 08 공학관
		{500,150},  // 09 학생회관
		{520,220},  // 10 의생명2관
		{570,230},  // 11 항공관
		{630,200},  // 12 의생명관
		{90,50},    // 13 백양생활관
		{190,110},  // 14 체육관
		{330,90},   // 15 글로벌타운 A,B동
		{730,220},  // 16 산업보안실습동
		{290,100},  // 17 글로벌타운 C동
		{250,110},  // 18 종합강의동
		{220,110},  // 19 대학본부동
		{800,150},  // 20 대운동장
		{800,200}   // 21 항공정비실습동
	};

	int w[N + 1][N + 1];
	for (int i = 1; i <= N; i++) {
		for (int j = 1; j <= N; j++) {
			w[i][j] = (i == j ? 0 : INF);
		}
	}

	int limit = 140; // 한계치 (연결 제한) 일정 거리 이하인 건물만 연결

	for (int i = 1; i <= N; i++) {
		for (int j = 1; j <= N; j++) {
			if (i == j) continue;
			int a = dist(p[i], p[j]);
			if (a <= limit) {
				w[i][j] = a;
			}
			else {
				w[i][j] = INF;
			}
		}
	}

	int start, end;

	while (1) {
		cout << "출발 건물 선택 (1~21) : ";
		cin >> start;
		if (cin.fail() || start < 1 || start > 21) { // start에 정수가 아닌 요소가 입력될 경우를 대비해서 .fail() 사용
			cout << "정수로 1~21 사이의 숫자를 다시 입력해보세요.\n";
		}
		else break;
	}

	while (1) {
		cout << "도착 건물 선택 (1~21) : ";
		cin >> end;
		if (cin.fail() || end < 1 || end > 21) {
			cout << "정수로 1~21 사이의 숫자를 다시 입력해보세요.\n";
		}
		else break;
	}

	int length[N + 1], touch[N + 1];
	dijkstra(N, w, start, length, touch);
	// length[i] : start -> i의 최단 거리 touch[i] : i로 오기 직전의 건물

	if (length[end] == INF) {
		cout << "경로를 탐색하는데 실패했습니다.\n";
		return 0;
	}
	
	cout << "최단 거리 : " << length[end] << endl;

	vector<int>path;
	
	// 부모 포인터(touch)를 사용해서 도착점에서 출발점까지 경로 복원
	for (int i = end; i != start; i = touch[i]) {
		path.push_back(i);
	}
	path.push_back(start);

	cout << "경로 : ";
	for (int i = path.size() - 1; i >= 0; i--) {
		cout << path[i] << (i ? " -> " : "\n");
	}

	return 0;
}