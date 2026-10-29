#include <iostream>
using namespace std;

int bin2(int n, int k) {
	unsigned int ** B = new unsigned int* [n + 1];
	// 더블 포인터 사용 각각 행 포인터 ( 배열 ), 행의 데이터를 가리키는 포인터

	for (int i = 0; i <= n; i++) {
		B[i] = new unsigned int[k + 1](); // ()= 0으로 촉화
	}

	for (int i = 0; i <= n; i++) {
		for (int j = 0; j <= k && j <= i; j++) {
			if (j == 0 || j == i) {
				B[i][j] = 1;
			}
			else {
				B[i][j] = B[i - 1][j - 1] + B[i - 1][j];
			}
		}
	}
	unsigned int result = B[n][k];
	return result;
}

int main()
{
	int n, k;
	cout << "n 입력 : ";
	cin >> n;
	cout << "k 입력 : ";
	cin >> k;

	cout << "C(" << n << "," << k << ") = " << bin2(n, k) << endl;

	return 0;
}

